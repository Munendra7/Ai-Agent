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

        //[KernelFunction("summarize_document"), Description("Use this function ONLY when the user explicitly asks to summarize a document and provides a document name. Do NOT use this for general document queries.")]
        //public async Task<string> SummarizeDocumentAsync([Description("Name of the document to summarize")] string documentName)
        //{
        //    try
        //    {
        //        var documentChunks = await embeddingService.RetrieveDocumentChunksAsync(documentName);

        //        // Step 2: Summarize each chunk
        //        var chunkSummaries = new List<string>();
        //        foreach (var chunk in documentChunks)
        //        {
        //            string summary = await SummarizeChunkAsync(chunk);
        //            chunkSummaries.Add(summary);
        //        }

        //        // Step 3: Generate final summary from chunk summaries
        //        string combinedSummary = string.Join("\n", chunkSummaries);
        //        string finalSummary = await SummarizeChunkAsync(combinedSummary);

        //        return finalSummary;
        //    }
        //    catch (Exception)
        //    {
        //        return "Error retrieving and summarizing document.";
        //    }
        //}

        //private async Task<string> SummarizeChunkAsync(string chunk)
        //{
        //    var promptTemplate = """
        //        Summarize the following text while keeping key details:
        //        {{$chunk}}

        //        Provide a concise summary within 300 words.
        //    """;

        //    var semanticFunction = _kernel.CreateFunctionFromPrompt(promptTemplate);

        //    var summaryResponse = await _kernel.InvokeAsync(
        //        semanticFunction,
        //        new KernelArguments { ["chunk"] = chunk }
        //    ).ConfigureAwait(false);

        //    return summaryResponse.ToString();
        //}

        [KernelFunction("summarize_document"), Description("Use this function ONLY when the user explicitly asks to summarize a document and provides a document name. Do NOT use this for general document queries.")]
        public async Task<string> SummarizeDocumentAsync([Description("Name of the document to summarize")] string documentName)
        {
            try
            {
                var documentChunks = await embeddingService.RetrieveDocumentChunksAsync(documentName);

                const int MaxTokensPerBatch = 4000;

                var groupedBatches = GroupChunksByTokenLimit(documentChunks, MaxTokensPerBatch);

                // Step 1: Summarize each batch in parallel
                var summarizationTasks = groupedBatches.Select(batch =>
                {
                    string batchText = string.Join("\n", batch);
                    return SummarizeChunkWithRetryAsync(batchText);
                });

                var intermediateSummaries = (await Task.WhenAll(summarizationTasks)).ToList();

                // Step 2: Recursively summarize if too large
                string finalSummary = await SummarizeRecursivelyAsync(intermediateSummaries, MaxTokensPerBatch);

                return finalSummary;
            }
            catch (Exception ex)
            {
                return $"Error summarizing document: {ex.Message}";
            }
        }

        private async Task<string> SummarizeRecursivelyAsync(List<string> summaries, int maxTokens)
        {
            while (true)
            {
                int totalTokens = summaries.Sum(s => EstimateTokenCount(s));
                if (totalTokens <= maxTokens)
                    return string.Join("\n", summaries);

                var grouped = GroupChunksByTokenLimit(summaries, maxTokens);

                var summarized = new List<string>();
                foreach (var group in grouped)
                {
                    string batch = string.Join("\n", group);
                    summarized.Add(await SummarizeChunkWithRetryAsync(batch));
                }

                summaries = summarized;
            }
        }

        private async Task<string> SummarizeChunkWithRetryAsync(string chunk, int maxRetries = 3)
        {
            int attempt = 0;
            while (attempt < maxRetries)
            {
                try
                {
                    return await SummarizeChunkAsync(chunk);
                }
                catch
                {
                    attempt++;
                    await Task.Delay(2000); // Wait for 2 seconds before retry
                }
            }
            return "[Failed to summarize after retries]";
        }

        private async Task<string> SummarizeChunkAsync(string chunk)
        {
            var promptTemplate = """
                Summarize the following text while preserving key details:
                {{$chunk}}

                Keep the summary concise, under 500 words.
            """;

            var semanticFunction = _kernel.CreateFunctionFromPrompt(promptTemplate);

            var summaryResponse = await _kernel.InvokeAsync(
                semanticFunction,
                new KernelArguments { ["chunk"] = chunk }
            ).ConfigureAwait(false);

            return summaryResponse.ToString();
        }

        private List<List<string>> GroupChunksByTokenLimit(IEnumerable<string> chunks, int maxTokens)
        {
            var batches = new List<List<string>>();
            var currentBatch = new List<string>();
            int currentTokenCount = 0;

            foreach (var chunk in chunks)
            {
                int chunkTokens = EstimateTokenCount(chunk);
                if (currentTokenCount + chunkTokens > maxTokens && currentBatch.Any())
                {
                    batches.Add(currentBatch);
                    currentBatch = new List<string>();
                    currentTokenCount = 0;
                }

                currentBatch.Add(chunk);
                currentTokenCount += chunkTokens;
            }

            if (currentBatch.Any())
                batches.Add(currentBatch);

            return batches;
        }

        private int EstimateTokenCount(string text) => text.Length / 4;
    }
}