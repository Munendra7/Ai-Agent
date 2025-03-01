using Microsoft.SemanticKernel;
using SemanticKernel.AIAgentBackend.Plugins;
using SemanticKernel.AIAgentBackend.Repositories;

namespace SemanticKernel.AIAgentBackend.Services
{
    public class SearchService : ISearchService
    {
        private readonly IKernelService _kernelService;
        private readonly GoogleSearchPlugin _googleSearchPlugin;

        public SearchService(IKernelService kernelService, GoogleSearchPlugin googleSearchPlugin)
        {
            _kernelService = kernelService;
            _googleSearchPlugin = googleSearchPlugin;
        }

        public async Task<string> GetSemanticSearchResponseAsync(string query, Kernel kernel)
        {
            // Import Google Search Plugin
            kernel.ImportPluginFromObject(_googleSearchPlugin, "google");

            // Invoke search
            var function = kernel.Plugins["google"]["search"];
            var searchResult = await kernel.InvokeAsync(function, new() { ["query"] = query });

            // Generate semantic response
            var semanticFunction = kernel.CreateFunctionFromPrompt(
                "Generate a concise, informative response based on the following search result:\n\n{{$searchResult}}\n\nEnsure the response is clear and engaging."
            );

            var finalResponse = await kernel.InvokeAsync(semanticFunction, new() { ["searchResult"] = searchResult.ToString() });

            return finalResponse.ToString();
        }
    }
}
