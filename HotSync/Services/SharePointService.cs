using System;
using System.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

namespace HotSync.Services
{
    public class SharePointService
    {
        private readonly string _clientId;
        private readonly string _tenantId;
        private readonly string _clientSecret;
        private readonly string _siteUrl; 

        
        private static string _cachedSiteId;

        public SharePointService()
        {
            _clientId = ConfigurationManager.AppSettings["ClientId"];
            _tenantId = ConfigurationManager.AppSettings["TenantId"];
            _clientSecret = ConfigurationManager.AppSettings["ClientSecret"];

         
            _siteUrl = ConfigurationManager.AppSettings["SiteUrl"];
        }

        public async Task<string> GetAccessTokenAsync()
        {
            var app = ConfidentialClientApplicationBuilder.Create(_clientId)
                .WithClientSecret(_clientSecret)
                .WithAuthority(new Uri($"https://login.microsoftonline.com/{_tenantId}"))
                .Build();

            string[] scopes = new string[] { "https://graph.microsoft.com/.default" };
            var result = await app.AcquireTokenForClient(scopes).ExecuteAsync();
            return result.AccessToken;
        }

       
        public string GetSiteId()
        {
            // If we already found it, return the cached version
            if (!string.IsNullOrEmpty(_cachedSiteId)) return _cachedSiteId;

            _cachedSiteId = FetchSiteIdFromGraph().GetAwaiter().GetResult();
            return _cachedSiteId;
        }

        private async Task<string> FetchSiteIdFromGraph()
        {
            string token = await GetAccessTokenAsync();
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // API format: https://graph.microsoft.com/v1.0/sites/{host-name}:/{server-relative-path}
                string requestUrl = $"https://graph.microsoft.com/v1.0/sites/{_siteUrl}";

                var response = await client.GetAsync(requestUrl);
                string json = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Failed to get Site ID. URL: {_siteUrl}. Error: {json}");
                }

                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    if (doc.RootElement.TryGetProperty("id", out JsonElement idElement))
                    {
                        return idElement.GetString();
                    }
                    throw new Exception("Graph response did not contain a Site ID.");
                }
            }
        }

     
        public string GetInitialGraphUrl(DateTime lastSync)
        {
            return null;
        }
    }
}