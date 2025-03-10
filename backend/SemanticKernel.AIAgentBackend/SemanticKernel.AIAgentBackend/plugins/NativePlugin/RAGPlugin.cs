using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Text;
using SemanticKernel.AIAgentBackend.Repositories.Interface;
using OpenAI.Chat;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace SemanticKernel.AIAgentBackend.plugins.NativePlugin
{
    public class RAGPlugin
    {
        private readonly Kernel _kernel;
        private readonly IEmbeddingService embeddingService;

        public RAGPlugin([FromKeyedServices("LLMKernel")] Kernel kernel, IEmbeddingService embeddingService)
        {
            _kernel = kernel;
            this.embeddingService = embeddingService;
        }

        [KernelFunction("answer"), Description("Acts as the AI knowledge base by retrieving relevant information from user-provided documents using a Retrieval-Augmented Generation (RAG) approach. It generates precise and context-aware answers based on the user's query.")]
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
                
                var promptTemplate = """
                    Context: {{$searchResults}}
                    Question: {{$query}}
                    Provide a clear and concise response (max 40 words) only from Context.
                """;

                var semanticFunction = _kernel.CreateFunctionFromPrompt(promptTemplate);

                var finalResponse = await _kernel.InvokeAsync(
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