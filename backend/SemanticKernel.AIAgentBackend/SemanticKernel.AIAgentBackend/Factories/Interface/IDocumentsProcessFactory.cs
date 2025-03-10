namespace SemanticKernel.AIAgentBackend.Factories.Interface
{
    public interface IDocumentsProcessFactory
    {
        IEnumerable<string> ExtractTextChunksFromFile(IFormFile file, int chunkSize = 512);
    }
}
