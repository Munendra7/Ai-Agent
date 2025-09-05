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
        private readonly IConfiguration _configuration;

        public RAGPlugin([FromKeyedServices("LLMKernel")] Kernel kernel, IEmbeddingService embeddingService, IBlobService blobService, IConfiguration configuration)
        {
            _kernel = kernel;
            this.embeddingService = embeddingService;
            this.blobService = blobService;
            _configuration = configuration;
        }

        [KernelFunction("answerfromKnowledge"), Description("Acts as the AI knowledge base by retrieving relevant information from user-provided information and documents using a Retrieval-Augmented Generation (RAG) approach. It generates precise and context-aware answers based on the user's query.")]
        public async Task<string> AnswerAsync([Description("User query")] string query, [Description("Optional: restrict search to specific document name")] string? documentname = null)
        {
            try
            {
                // Enhance query if configured
                var enhanceQuery = _configuration.GetValue<bool>("RAG:EnableQueryEnhancement", true);
                var searchQuery = enhanceQuery ? await EnhanceQuery(query) : query;

                // Get configuration values
                var topK = _configuration.GetValue<int>("RAG:MaxSearchResults", 5);
                var minScore = _configuration.GetValue<float>("RAG:MinRelevanceScore", 0.7f);

                var searchResults = string.IsNullOrEmpty(documentname)
                ? await embeddingService.SimilaritySearch(query, topK, minScore).ConfigureAwait(false)
                : await embeddingService.SimilaritySearchInFile(query, documentname, topK, minScore).ConfigureAwait(false);

                // Filter by relevance score
                var relevantResults = searchResults
                    .Where(r => r.Score > minScore)
                    .Take(topK)
                    .ToList();

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
                        builder.AppendLine($"[SourceName]:  {fileName.StringValue} | [Context]: {chunk.StringValue}");
                    }
                }
                string searchResultsText = builder.ToString();

                // Dynamic prompt template with source references
                string promptTemplate = @"
                You are a highly accurate AI assistant. Answer the user's question **strictly using the provided context**.

                --- CONTEXT DOCUMENTS ---
                {{$searchResults}}

                Each context chunk is labeled with its source like [SourceName]: <text>. Always reference the source name when using information.

                --- USER QUESTION ---
                {{$query}}

                --- INSTRUCTIONS ---
                1. Answer **only** using the information provided in the context above.
                2. If the context does not contain sufficient information, explicitly state what is missing.
                3. For every fact or statement, include the source document name in square brackets, e.g., [Doc1].
                4. Be concise but complete (maximum 300 words).
                5. Structure your answer clearly using paragraphs or bullet points.
                6. Do **not** fabricate information or sources.

                --- RESPONSE ---
                Your answer (with sources referenced): <Answer>
                Reference Sources: <SourceName(s)>";


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

        [KernelFunction("download_document"), Description("Download a document from AI Agent Knowledge and get the SAS URL of the file.")]
        public string DownloadDocumentAsync([Description("Name of the document to download")] string documentName)
        {
            var sasuri = blobService.GenerateSasUri(documentName, BlobStorageConstants.KnowledgeContainerName);

            return sasuri.Replace(BlobStorageConstants.StorageImageName, "localhost");
        }

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

                return $"## Summary of {documentName}\n\n{finalSummary}";
            }
            catch (Exception ex)
            {
                return $"Error summarizing document: {ex.Message}";
            }
        }

        private async Task<string> EnhanceQuery(string originalQuery)
        {
            var enhancementPrompt = @"
            Rewrite the following query to be more comprehensive for semantic search.
            Include relevant synonyms and related terms while maintaining the original intent.
            Keep the enhanced query concise (max 2-3 sentences).

            Original query: {{$query}}

            Enhanced query:";

            var semanticFunction = _kernel.CreateFunctionFromPrompt(enhancementPrompt);

            var enhancedQuery = await _kernel.InvokeAsync(
                semanticFunction,
                new KernelArguments { ["query"] = originalQuery }
            ).ConfigureAwait(false);

            return enhancedQuery.ToString();
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
            Create a structured summary of the following text:

            TEXT:
            {{$chunk}}

            SUMMARY REQUIREMENTS:
            - Capture ALL key facts and figures
            - Maintain chronological order if applicable
            - Preserve technical terms and proper nouns
            - Use bullet points for lists
            - Maximum 500 words

            SUMMARY:
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