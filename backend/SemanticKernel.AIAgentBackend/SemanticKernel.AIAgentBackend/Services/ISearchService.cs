using Microsoft.SemanticKernel;

namespace SemanticKernel.AIAgentBackend.Services
{
    public interface ISearchService
    {
        Task<string> GetSemanticSearchResponseAsync(string query, Kernel kernel);
    }
}
