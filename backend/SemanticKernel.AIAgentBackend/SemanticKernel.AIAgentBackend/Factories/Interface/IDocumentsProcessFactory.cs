using Newtonsoft.Json.Linq;

namespace SemanticKernel.AIAgentBackend.Factories.Interface
{
    public interface IDocumentsProcessFactory
    {
        IEnumerable<string> ExtractTextChunksFromFile(IFormFile file, int chunkSize = 512);

        IEnumerable<string> ChunkText(string text, int chunkSize);

        HashSet<string> ExtractPlaceholders(Stream templateStream);

        MemoryStream ReplacePlaceholdersInDocx(Stream templateStream, Dictionary<string, object> dynamicInputs);

        JObject ExtractRequiredPayload(Stream templateStream);

        MemoryStream PopulateContentControlsFromJson(Stream templateStream, string jsonPayload);
    }
}
