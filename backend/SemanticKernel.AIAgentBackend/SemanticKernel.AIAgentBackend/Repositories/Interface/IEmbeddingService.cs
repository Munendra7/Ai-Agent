using Qdrant.Client.Grpc;
using SemanticKernel.AIAgentBackend.Models.DTO;

namespace SemanticKernel.AIAgentBackend.Repositories.Interface
{
    public interface IEmbeddingService
    {
        Task<string> ProcessFileAsync(FileUploadDTO fileDTO, string filePath, List<string>? textChunks=null);
        Task<IReadOnlyList<ScoredPoint>> SimilaritySearch(string prompt);
        Task<IReadOnlyList<ScoredPoint>> SimilaritySearchInFile(string prompt, string fileName);
        Task<List<string>> GetAllDocumentsAsync();
        Task<List<string>> RetrieveDocumentChunksAsync(string documentName);
    }
}
