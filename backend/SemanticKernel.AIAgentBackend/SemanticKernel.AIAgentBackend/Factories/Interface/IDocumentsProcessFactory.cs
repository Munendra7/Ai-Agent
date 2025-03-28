namespace SemanticKernel.AIAgentBackend.Factories.Interface
{
    public interface IDocumentsProcessFactory
    {
        IEnumerable<string> ExtractTextChunksFromFile(IFormFile file, int chunkSize = 512);

        Dictionary<string, string> ExtractPlaceholders(Stream templateStream);

        MemoryStream ReplacePlaceholdersInDocx(Stream templateStream, Dictionary<string, string>? parameters, Dictionary<string, List<List<string>>>? tableInputs);
    }
}
