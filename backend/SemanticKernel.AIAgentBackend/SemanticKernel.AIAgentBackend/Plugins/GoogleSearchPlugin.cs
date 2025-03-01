using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;

namespace SemanticKernel.AIAgentBackend.Plugins
{
    public class GoogleSearchPlugin
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _searchEngineId;

        public GoogleSearchPlugin(HttpClient httpClient, string apiKey, string searchEngineId)
        {
            _httpClient = httpClient;
            _apiKey = apiKey;
            _searchEngineId = searchEngineId;
        }

        [KernelFunction("search")]
        public async Task<string> SearchAsync(string query)
        {
            string apiUrl = $"https://www.googleapis.com/customsearch/v1?q={Uri.EscapeDataString(query)}&key={_apiKey}&cx={_searchEngineId}";

            HttpResponseMessage response = await _httpClient.GetAsync(apiUrl);
            if (!response.IsSuccessStatusCode)
            {
                return "Error fetching search results.";
            }

            string jsonResult = await response.Content.ReadAsStringAsync();
            using JsonDocument doc = JsonDocument.Parse(jsonResult);
            JsonElement root = doc.RootElement;

            // Extract first search result
            if (root.TryGetProperty("items", out JsonElement items) && items.GetArrayLength() > 0)
            {
                var firstItem = items[0];
                if (firstItem.TryGetProperty("title", out JsonElement title) &&
                    firstItem.TryGetProperty("link", out JsonElement link))
                {
                    return $"{title.GetString()} ({link.GetString()})";
                }
            }

            return "No relevant search results found.";
        }
    }
}