using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI;
using SemanticKernel.AIAgentBackend.Repositories;
using System.ComponentModel;
using System.Text;
using System.Linq;
using System.Threading.Tasks;

namespace SemanticKernel.AIAgentBackend.plugins.NativePlugin
{
    public class RAGPlugin
    {
        private readonly IKernelService kernelService;
        private readonly IEmbeddingService embeddingService;

        public RAGPlugin(IKernelService kernelService, IEmbeddingService embeddingService)
        {
            this.kernelService = kernelService;
            this.embeddingService = embeddingService;
        }

        [KernelFunction("answer"), Description("Generates an answer from RAG based on user query.")]
        public async Task<string> AnswerAsync([Description("User query")] string query)
        {
            try
            {
                
                var searchResults = await embeddingService.SimilaritySearch(query).ConfigureAwait(false);

                if (searchResults == null || !searchResults.Any())
                {
                    return "I'm sorry, but I couldn't find relevant information to answer your query.";
                }

                
                var builder = new StringBuilder();
                foreach (var result in searchResults)
                {
                    if (result.Payload.TryGetValue("FileName", out var fileName) &&
                        result.Payload.TryGetValue("Chunk", out var chunk))
                    {
                        builder.AppendLine($"{fileName.StringValue}: {chunk.StringValue}");
                    }
                }

                string searchResultsText = builder.ToString();

                
                var ollamaKernel = this.kernelService.GetKernel("Ollama");
                
                var promptTemplate = """
                    Context: {{$searchResults}}
                    Question: {{$query}}
                    Provide a clear and concise response (max 50 words).
                """;

                var semanticFunction = ollamaKernel.CreateFunctionFromPrompt(promptTemplate);

                var finalResponse = await ollamaKernel.InvokeAsync(
                    semanticFunction,
                    new KernelArguments
                    {
                        ["query"] = query,
                        ["searchResults"] = searchResultsText
                    }
                ).ConfigureAwait(false);

                return finalResponse.ToString();
            }

            catch (Exception)
            {
                return "An unexpected error occurred. Please try again.";
            }
        }
    }
}