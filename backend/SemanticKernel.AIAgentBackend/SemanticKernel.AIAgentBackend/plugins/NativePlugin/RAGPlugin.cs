using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Text;
using SemanticKernel.AIAgentBackend.Repositories.Interface;
using SemanticKernel.AIAgentBackend.Constants;

namespace SemanticKernel.AIAgentBackend.plugins.NativePlugin
{
    public class RAGPlugin
    {
        private readonly Kernel _kernel;
        private readonly IEmbeddingService embeddingService;
        private readonly IBlobService blobService;

        public RAGPlugin([FromKeyedServices("LLMKernel")] Kernel kernel, IEmbeddingService embeddingService, IBlobService blobService)
        {
            _kernel = kernel;
            this.embeddingService = embeddingService;
            this.blobService = blobService;
        }

        [KernelFunction("answerfromKnowledge"), Description("Acts as the AI knowledge base by retrieving relevant information from user-provided information and documents using a Retrieval-Augmented Generation (RAG) approach. It generates precise and context-aware answers based on the user's query.")]
        public async Task<string> AnswerAsync([Description("User query")] string query, [Description("Optional: restrict search to specific document name")] string? documentname = null)
        {
            try
            {
                //var searchResults = await embeddingService.SimilaritySearch(query).ConfigureAwait(false);
                var searchResults = string.IsNullOrEmpty(documentname)
                ? await embeddingService.SimilaritySearch(query).ConfigureAwait(false)
                : await embeddingService.SimilaritySearchInFile(query, documentname).ConfigureAwait(false);

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
                string promptTemplate = @"
                    You are an AI assistant that answers questions based only on the given context.
    
                    Context: {{$searchResults}}
                    Question: {{$query}}
    
                    Provide a clear and concise response (maximum 300 words) strictly based on the provided Context.  
                    Mention the file name where it is located.'
                ";

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

        [KernelFunction("list_documents"), Description("Lists all stored documents in AI Agent Knowledge.")]
        public async Task<List<string>> ListDocumentsAsync()
        {
            //return await embeddingService.GetAllDocumentsAsync();
            var knowledgeDocuments = await blobService.ListFilesAsync(BlobStorageConstants.KnowledgeContainerName);

            return knowledgeDocuments;
        }

        [KernelFunction("summarize_document"), Description("Use this function ONLY when the user explicitly asks to summarize a document and provides a document name. Do NOT use this for general document queries.")]
        public async Task<string> SummarizeDocumentAsync([Description("Name of the document to summarize")] string documentName)
        {
            try
            {
                var documentChunks = await embeddingService.RetrieveDocumentChunksAsync(documentName);

                // Step 2: Summarize each chunk
                var chunkSummaries = new List<string>();
                foreach (var chunk in documentChunks)
                {
                    string summary = await SummarizeChunkAsync(chunk);
                    chunkSummaries.Add(summary);
                }

                // Step 3: Generate final summary from chunk summaries
                string combinedSummary = string.Join("\n", chunkSummaries);
                string finalSummary = await SummarizeChunkAsync(combinedSummary);

                return finalSummary;
            }
            catch (Exception)
            {
                return "Error retrieving and summarizing document.";
            }
        }

        private async Task<string> SummarizeChunkAsync(string chunk)
        {
            var promptTemplate = """
                Summarize the following text while keeping key details:
                {{$chunk}}

                Provide a concise summary within 300 words.
            """;

            var semanticFunction = _kernel.CreateFunctionFromPrompt(promptTemplate);

            var summaryResponse = await _kernel.InvokeAsync(
                semanticFunction,
                new KernelArguments { ["chunk"] = chunk }
            ).ConfigureAwait(false);

            return summaryResponse.ToString();
        }
    }
}