namespace SemanticKernel.AIAgentBackend.Factories.Interface
{
    public interface IDocumentsProcessFactory
    {
        IEnumerable<string> ExtractTextChunksFromFile(IFormFile file, int chunkSize = 512);

        HashSet<string> ExtractPlaceholders(Stream templateStream);

        MemoryStream ReplacePlaceholdersInDocx(Stream templateStream, Dictionary<string, object> dynamicInputs);
    }
}
