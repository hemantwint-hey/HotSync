using HotSync.Models;
using System;
using System.Text.Json;

namespace HotSync.Helpers
{
    public static class RigDetailsMapper
    {
        public static RigDetailsData Map(JsonElement item)
        {
            try
            {
                if (!item.TryGetProperty("fields", out JsonElement fields))
                    throw new Exception("Missing fields");

                var data = new RigDetailsData
                {
                    Id = GetRootInt(item, "id"),
                    Title = GetString(fields, "Title"),
                    RigName = GetString(fields, "Rig_Name"),
                    RigLoginName = GetString(fields, "Rig_Login_Name"),
                    RigId = GetInt(fields, "Rig_Id"),
                    Active = GetBool(fields, "Active"),
                    Flag = GetString(fields, "Flag"),

                    // Dates
                    Created = GetDate(item, "createdDateTime"), 
                    Modified = GetDate(item, "lastModifiedDateTime"),

                
                    CreatedById = GetInt(fields, "AuthorLookupId"), 
                    CreatedByName = GetUserProp(item, "createdBy", "displayName"),
                    CreatedByEmail = GetUserProp(item, "createdBy", "email"),

                   
                    ModifiedById = GetInt(fields, "EditorLookupId"), 
                    ModifiedByName = GetUserProp(item, "lastModifiedBy", "displayName"),
                    ModifiedByEmail = GetUserProp(item, "lastModifiedBy", "email")
                };

                return data;
            }
            catch (Exception ex)
            {
                throw new Exception($"Map Error: {ex.Message}");
            }
        }

      
        private static string GetUserProp(JsonElement root, string parentField, string childProp)
        {
           
            if (root.TryGetProperty(parentField, out JsonElement parent) &&
                parent.TryGetProperty("user", out JsonElement user) &&
                user.TryGetProperty(childProp, out JsonElement val))
            {
                return val.GetString();
            }
            return "Unknown";
        }

        private static int GetRootInt(JsonElement root, string key)
        {
            if (root.TryGetProperty(key, out JsonElement val))
            {
                if (val.ValueKind == JsonValueKind.Number) return val.GetInt32();
                if (val.ValueKind == JsonValueKind.String && int.TryParse(val.GetString(), out int i)) return i;
            }
            return 0;
        }

        private static string GetString(JsonElement fields, string key) =>
            fields.TryGetProperty(key, out var v) ? v.GetString() : null;

        private static int GetInt(JsonElement fields, string key)
        {
            if (fields.TryGetProperty(key, out var v))
            {
                if (v.ValueKind == JsonValueKind.Number) return (int)v.GetDouble();
                if (v.ValueKind == JsonValueKind.String && double.TryParse(v.GetString(), out double d)) return (int)d;
            }
            return 0;
        }

        private static bool GetBool(JsonElement fields, string key)
        {
            if (fields.TryGetProperty(key, out var v))
            {
                if (v.ValueKind == JsonValueKind.True) return true;
                if (v.ValueKind == JsonValueKind.String && bool.TryParse(v.GetString(), out bool b)) return b;
            }
            return false;
        }

        private static DateTime GetDate(JsonElement root, string key) =>
            root.TryGetProperty(key, out var v) && DateTime.TryParse(v.GetString(), out var d) ? d : new DateTime(1900, 1, 1);
    }
}