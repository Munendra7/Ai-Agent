using Microsoft.AspNetCore.Mvc;
using Qdrant.Client.Grpc;
using SemanticKernel.AIAgentBackend.Models.DTO;

namespace SemanticKernel.AIAgentBackend.Repositories.Interface
{
    public interface IEmbeddingService
    {
        Task<string> ProcessFileAsync(FileUploadDTO fileDTO, string filePath);
        Task<IReadOnlyList<ScoredPoint>> SimilaritySearch(string prompt);
        Task<List<string>> GetAllDocumentsAsync();
        Task<List<string>> RetrieveDocumentChunksAsync(string documentName);
    }
}
