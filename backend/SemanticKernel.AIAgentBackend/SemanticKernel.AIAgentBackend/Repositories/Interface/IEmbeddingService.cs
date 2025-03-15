using Microsoft.AspNetCore.Mvc;
using Qdrant.Client.Grpc;
using SemanticKernel.AIAgentBackend.Models.DTO;

namespace SemanticKernel.AIAgentBackend.Repositories.Interface
{
    public interface IEmbeddingService
    {
        Task<IActionResult> ProcessFileAsync(FileUploadDTO fileDTO);
        Task<IReadOnlyList<ScoredPoint>> SimilaritySearch(string prompt);
        Task<List<string>> GetAllDocumentsAsync();
        Task<List<string>> RetrieveDocumentChunksAsync(string documentName);
    }
}
