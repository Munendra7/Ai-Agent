using System;
using System.ComponentModel;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.OpenApi.Services;
using Microsoft.SemanticKernel;
using SemanticKernel.AIAgentBackend.Repositories;

namespace SemanticKernel.AIAgentBackend.plugins.NativePlugin
{
    public class GoogleSearchPlugin
    {
        private readonly HttpClient _httpClient;
        private readonly IKernelService _kernel;
        private readonly string _modelName;
        private readonly IConfiguration _configuration;

        public GoogleSearchPlugin(IConfiguration configuration, IKernelService kernel, string modelName)
        {
            _httpClient = new HttpClient();
            _kernel = kernel;
            _modelName = modelName;
            _configuration = configuration;
        }

        [KernelFunction("search"), Description("Searches the web based on user query")]
        public async Task<string> SearchAsync([Description("user query")] string query)
        {
            string _apiKey = _configuration["GoogleSearch:ApiKey"]!;
            string _searchEngineId = _configuration["GoogleSearch:SearchEngineId"]!;

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
                    var searchResult = $"{title.GetString()} ({link.GetString()})";
                    // Generate semantic response

                    var kernel = _kernel.GetKernel(_modelName);
                    var semanticFunction = kernel.CreateFunctionFromPrompt(
                        "Based on user query {{$query}} Generate a concise, informative response based on the following search result:\n\n{{$searchResult}}\n\nEnsure the response is clear and engaging."
                    );

                    var finalResponse = await kernel.InvokeAsync(semanticFunction, new() { ["searchResult"] = searchResult.ToString(), ["query"] = query });

                    return finalResponse.ToString();
                }
            }

            return "No relevant search results found.";
        }
    }
}