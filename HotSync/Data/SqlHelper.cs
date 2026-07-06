using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using HotSync.Models;

namespace HotSync.Data
{
    public class SqlHelper
    {
        private readonly string _connString;

        public SqlHelper()
        {
            _connString = ConfigurationManager.ConnectionStrings["SQLConnection"].ConnectionString;
        }

        
       
        public void LogAudit(int runId, int sharePointId, string actionType, string details)
        {
            using (var conn = new SqlConnection(_connString))
            {
                var cmd = new SqlCommand(@"
                    INSERT INTO HotSync_Audit_Log (RunId, SharePoint_Id, Action_Type, Event_Time, Details)
                    VALUES (@RunId, @SpId, @Action, GETUTCDATE(), @Details)", conn);

                cmd.Parameters.AddWithValue("@RunId", runId);
                cmd.Parameters.AddWithValue("@SpId", sharePointId);
                cmd.Parameters.AddWithValue("@Action", actionType);
                cmd.Parameters.AddWithValue("@Details", details ?? (object)DBNull.Value);

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        
        // DELTA STATE
        
        public string GetStoredDeltaLink(string sourceKey)
        {
            using (var conn = new SqlConnection(_connString))
            {
                var cmd = new SqlCommand("SELECT DeltaLink FROM HotSync_Sync_State WHERE SourceKey = @Src", conn);
                cmd.Parameters.AddWithValue("@Src", sourceKey);
                conn.Open();
                var r = cmd.ExecuteScalar();
                return r == DBNull.Value ? null : (string)r;
            }
        }

        public void SaveDeltaLink(string sourceKey, string deltaLink)
        {
            using (var conn = new SqlConnection(_connString))
            {
                var cmd = new SqlCommand(@"
                    MERGE HotSync_Sync_State AS T
                    USING (SELECT @Src AS SourceKey) S
                    ON T.SourceKey = S.SourceKey
                    WHEN MATCHED THEN
                        UPDATE SET DeltaLink = @Link, Last_Synced = GETUTCDATE()
                    WHEN NOT MATCHED THEN
                        INSERT (SourceKey, DeltaLink, Last_Synced)
                        VALUES (@Src, @Link, GETUTCDATE());", conn);

                cmd.Parameters.AddWithValue("@Src", sourceKey);
                cmd.Parameters.AddWithValue("@Link", deltaLink);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        
    
        public void UpsertRig(RigDetailsData d, int runId)
        {
            using (var conn = new SqlConnection(_connString))
            using (var cmd = new SqlCommand("Upsert_RigDetails", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@Id", d.Id);
                cmd.Parameters.AddWithValue("@Title", (object)d.Title ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@RigName", (object)d.RigName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@RigLoginName", (object)d.RigLoginName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@RigId", d.RigId);
                cmd.Parameters.AddWithValue("@Active", d.Active);

                cmd.Parameters.AddWithValue("@Created", d.Created);
                cmd.Parameters.AddWithValue("@CreatedById", d.CreatedById);
                cmd.Parameters.AddWithValue("@CreatedByName", (object)d.CreatedByName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@CreatedByEmail", (object)d.CreatedByEmail ?? DBNull.Value);

                cmd.Parameters.AddWithValue("@Modified", d.Modified);
                cmd.Parameters.AddWithValue("@ModifiedById", d.ModifiedById);
                cmd.Parameters.AddWithValue("@ModifiedByName", (object)d.ModifiedByName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ModifiedByEmail", (object)d.ModifiedByEmail ?? DBNull.Value);

                cmd.Parameters.AddWithValue("@Flag", "U");
                cmd.Parameters.AddWithValue("@SyncCount", runId);

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

       
        // SOFT DELETE (Single Item)
      
        public void SoftDeleteOne(int rigId, int runId)
        {
            using (var conn = new SqlConnection(_connString))
            {
                var cmd = new SqlCommand(@"
                    UPDATE RigDetails
                    SET 
                        Flag = 'D', 
                        Active = 0, 
                        Sync_Count = @RunId,
                        Last_Updated_By_Sync = GETUTCDATE()
                    WHERE Id = @Id", conn);

                cmd.Parameters.AddWithValue("@Id", rigId);
                cmd.Parameters.AddWithValue("@RunId", runId);

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        
        public int StartRun(string sourceKey, string userName)
        {
            using (var conn = new SqlConnection(_connString))
            using (var cmd = new SqlCommand("HotSync_Run_Start", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@SourceKey", sourceKey);
                cmd.Parameters.AddWithValue("@TriggeredBy", userName);
                conn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public void EndRun(int runId, string status, string message)
        {
            using (var conn = new SqlConnection(_connString))
            using (var cmd = new SqlCommand("HotSync_Run_End", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@RunId", runId);
                cmd.Parameters.AddWithValue("@Status", status);
                cmd.Parameters.AddWithValue("@Message", message);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }
    }
}