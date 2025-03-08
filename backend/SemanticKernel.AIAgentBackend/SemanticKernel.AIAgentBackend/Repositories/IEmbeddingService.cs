using Microsoft.AspNetCore.Mvc;
using Qdrant.Client.Grpc;
using SemanticKernel.AIAgentBackend.Models.DTO;

namespace SemanticKernel.AIAgentBackend.Repositories
{
    public interface IEmbeddingService
    {
        Task<IActionResult> ProcessFileAsync(FileUploadDTO fileDTO);
        Task<IReadOnlyList<ScoredPoint>> SimilaritySearch(string prompt);
    }
}
