using Qdrant.Client.Grpc;
using SemanticKernel.AIAgentBackend.Models.DTO;

namespace SemanticKernel.AIAgentBackend.Repositories.Interface
{
    public interface IEmbeddingService
    {
        Task<string> ProcessFileAsync(FileUploadDTO fileDTO, string filePath, List<string>? textChunks=null);
        Task<IReadOnlyList<ScoredPoint>> SimilaritySearch(string prompt, int topK = 5, float scoreThreshold = 0.7f);
        Task<IReadOnlyList<ScoredPoint>> SimilaritySearchInFile(string prompt, string fileName, int topK = 5, float scoreThreshold = 0.7f);
        Task<List<string>> GetAllDocumentsAsync();
        Task<List<string>> RetrieveDocumentChunksAsync(string documentName);
    }
}
