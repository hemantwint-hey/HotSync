using System;
using System.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using HotSync.Data;
using HotSync.Helpers;
using HotSync.Services;

namespace HotSync
{
    class Program
    {
        static async Task Main(string[] args)
        {
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

            var sql = new SqlHelper();
            string listName = ConfigurationManager.AppSettings["ListName"];
            string sourceKey = listName;
            string user = Environment.UserName;

            int runId = sql.StartRun(sourceKey, user);

            int countU = 0; 
            int countD = 0; 

            try
            {
                var sp = new SharePointService();
                string token = await sp.GetAccessTokenAsync();
                string siteId = sp.GetSiteId();

                string delta = sql.GetStoredDeltaLink(sourceKey);
                string url = string.IsNullOrEmpty(delta)
                    ? $"https://graph.microsoft.com/v1.0/sites/{siteId}/lists/{listName}/items/delta?expand=fields"
                    : delta;

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                    while (!string.IsNullOrEmpty(url))
                    {
                        var res = await client.GetAsync(url);
                        var json = await res.Content.ReadAsStringAsync();

                        if (!res.IsSuccessStatusCode)
                            throw new Exception($"Graph API Failed: {json}");

                        using (var doc = JsonDocument.Parse(json))
                        {
                            var root = doc.RootElement;

                            if (root.TryGetProperty("value", out JsonElement valueElement))
                            {
                                foreach (var item in valueElement.EnumerateArray())
                                {
                                    int id = int.Parse(item.GetProperty("id").GetString());

                                
                                    if (item.TryGetProperty("@removed", out _))
                                    {
                                        sql.SoftDeleteOne(id, runId);
                                        countD++;

                                        
                                        sql.LogAudit(runId, id, "DELETE", "Graph API @removed event");
                                        Console.WriteLine($"[DELETE] (Explicit) ID: {id}");
                                    }
                                    else if (item.TryGetProperty("fields", out _))
                                    {
                                        // GHOST UPDATE CHECK
                                        bool reallyExists = await CheckIfItemExists(client, siteId, listName, id);

                                        if (reallyExists)
                                        {
                                            var data = RigDetailsMapper.Map(item);
                                            sql.UpsertRig(data, runId);
                                            countU++;

                                            sql.LogAudit(runId, id, "UPSERT", $"Title: {data.Title}");
                                            Console.WriteLine($"[UPDATE] Verified ID: {id}");
                                        }
                                        else
                                        {
                                            // Ghost Update -> Treat as Delete
                                            sql.SoftDeleteOne(id, runId);
                                            countD++;

                                            // LOGGING
                                            sql.LogAudit(runId, id, "DELETE", "Ghost Update (404 Not Found)");
                                            Console.WriteLine($"[DELETE] (Ghost Update Fixed) ID: {id}");
                                        }
                                    }
                                }
                            }

                            if (root.TryGetProperty("@odata.nextLink", out var next))
                                url = next.GetString();
                            else if (root.TryGetProperty("@odata.deltaLink", out var d))
                            {
                                sql.SaveDeltaLink(sourceKey, d.GetString());
                                url = null;
                            }
                            else
                                url = null;
                        }
                    }
                }

                sql.EndRun(runId, "Success", $"Updates: {countU}, Deletes: {countD}");
                Console.WriteLine($"\n--- SYNC COMPLETE ---\nUpdates: {countU}\nDeletes: {countD}");
            }
            catch (Exception ex)
            {
                sql.EndRun(runId, "Failed", ex.Message);
                Console.WriteLine($"\n--- SYNC ERROR ---\n{ex.Message}");
            }

            Console.ReadLine();
        }

        // Helper for Ghost Update Check
        private static async Task<bool> CheckIfItemExists(HttpClient client, string siteId, string listName, int itemId)
        {
            try
            {
                string checkUrl = $"https://graph.microsoft.com/v1.0/sites/{siteId}/lists/{listName}/items/{itemId}?$select=id";
                var response = await client.GetAsync(checkUrl);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}