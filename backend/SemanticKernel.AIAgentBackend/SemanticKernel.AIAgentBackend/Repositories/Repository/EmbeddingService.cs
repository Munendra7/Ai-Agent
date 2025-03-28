using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel.Embeddings;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using SemanticKernel.AIAgentBackend.Factories.Interface;
using SemanticKernel.AIAgentBackend.Models.DTO;
using SemanticKernel.AIAgentBackend.Repositories.Interface;
using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace SemanticKernel.AIAgentBackend.Repositories.Repository
{
    public class EmbeddingService : IEmbeddingService
    {
        private readonly QdrantClient _qdrantClient;
        private readonly IConfiguration _configuration;
        private readonly IDocumentsProcessFactory _documentsProcessFactory;
        #pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        private readonly ITextEmbeddingGenerationService _embeddingGenerator;
        #pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

        #pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        public EmbeddingService(ITextEmbeddingGenerationService embeddingGenerator, QdrantClient qdrantClient, IConfiguration configuration, IDocumentsProcessFactory documentsProcessFactory)
        #pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        {
            _qdrantClient = qdrantClient;
            _configuration = configuration;
            _documentsProcessFactory = documentsProcessFactory;
            _embeddingGenerator = embeddingGenerator;
        }

        public async Task<string> ProcessFileAsync(FileUploadDTO fileDTO, string filePath)
        {
            if (fileDTO == null || fileDTO.File.Length == 0)
            {
                return "Invalid file.";
            }

            List<string> textChunks = _documentsProcessFactory.ExtractTextChunksFromFile(fileDTO.File).ToList();
            if (textChunks.Count == 0)
            {
                return "Could not extract text from the file.";
            }

            // Generate embeddings for each chunk
            var embeddings = new List<float[]>();
            var chunkTexts = new List<string>();
            foreach (var chunk in textChunks)
            {
                var embedding = await _embeddingGenerator.GenerateEmbeddingAsync(chunk);
                embeddings.Add(embedding.ToArray());
                chunkTexts.Add(chunk);
            }

            // Store in Qdrant
            await StoreEmbeddingAsync(fileDTO.FileName, fileDTO.FileDescription, embeddings, chunkTexts, filePath);

            return "File processed and embeddings stored successfully.";
        }

        private async Task StoreEmbeddingAsync(string fileName, string? fileDescription, List<float[]> embeddings, List<string> chunkTexts, string filePath)
        {
            var collectionName = _configuration["Qdrant:CollectionName"] ?? "document_embeddings";
            ulong vectorSize = _configuration["Qdrant:VectorSize"] != null ? ulong.Parse(_configuration["Qdrant:VectorSize"]!) : 768;
            var points = new List<PointStruct>();
            for (int i = 0; i < embeddings.Count; i++)
            {
                var point = new PointStruct
                {
                    Id = new PointId { Uuid = Guid.NewGuid().ToString() },
                    Payload =
                    {
                        ["FileName"] = fileName,
                        ["FilePath"] = filePath,
                        ["FileDescription"] = fileDescription ?? string.Empty,
                        ["ChunkIndex"] = i.ToString(),
                        ["Chunk"] = chunkTexts[i]
                    },
                    Vectors = new Vectors { Vector = new Vector { Data = { embeddings[i] } } }
                };
                points.Add(point);
            }

            var existingCollections = await _qdrantClient.ListCollectionsAsync();
            if (!existingCollections.Any(name => name == collectionName))
            {
                await _qdrantClient.CreateCollectionAsync(collectionName, new VectorParams
                {
                    Size = vectorSize,
                    Distance = Distance.Cosine
                });
            }
            await _qdrantClient.UpsertAsync(collectionName, points);
        }

        public async Task<IReadOnlyList<ScoredPoint>> SimilaritySearch(string prompt)
        {
            var promptEmbedding = await _embeddingGenerator.GenerateEmbeddingAsync(prompt);
            var collectionName = _configuration["Qdrant:CollectionName"] ?? "document_embeddings";

            var returnedLocations = await _qdrantClient.QueryAsync(
                collectionName: collectionName,
                query: promptEmbedding.ToArray(),
                limit: 10
            );

            return returnedLocations;
        }

        public async Task<List<string>> GetAllDocumentsAsync()
        {
            var collectionName = _configuration["Qdrant:CollectionName"] ?? "document_embeddings";
            var allDocuments = new HashSet<string>();
            int limit = 100;
            PointId? nextOffset = null;

            do
            {
                var scrollResponse = await _qdrantClient.ScrollAsync(
                    collectionName,
                    filter: null,
                    limit: (uint)limit,
                    offset: nextOffset,
                    payloadSelector: new WithPayloadSelector { Enable = true }
                );

                if (scrollResponse?.Result == null || !scrollResponse.Result.Any())
                    break; // Stop if no more results

                foreach (var point in scrollResponse.Result)
                {
                    if (point.Payload.TryGetValue("FileName", out var fileName) && !string.IsNullOrEmpty(fileName.StringValue))
                        allDocuments.Add(fileName.StringValue);
                }

                nextOffset = scrollResponse.NextPageOffset;
            } while (nextOffset != null);

            return allDocuments.ToList();
        }

        public async Task<List<string>> RetrieveDocumentChunksAsync(string documentName)
        {
            var collectionName = _configuration["Qdrant:CollectionName"] ?? "document_embeddings";
            var allChunks = new List<(int ChunkIndex, string ChunkText)>();
            int limit = 100; // Fetch up to 100 chunks per request
            PointId? nextOffset = null;

            var filter = new Filter
            {
                Must = { new Condition
                {
                    Field = new FieldCondition
                    {
                        Key = "FileName",
                        Match = new Match
                        {
                            Text = documentName // Filter chunks by document name
                        }
                    }
                }}
            };

            do
            {
                var scrollResponse = await _qdrantClient.ScrollAsync(
                    collectionName,
                    filter: filter,
                    limit: (uint)limit,
                    offset: nextOffset,
                    payloadSelector: new WithPayloadSelector { Enable = true }
                );

                if (scrollResponse?.Result == null || !scrollResponse.Result.Any())
                    break;

                foreach (var point in scrollResponse.Result)
                {
                    if (point.Payload.TryGetValue("Chunk", out var chunk) &&
                        point.Payload.TryGetValue("ChunkIndex", out var chunkIndex) &&
                        !string.IsNullOrEmpty(chunk.StringValue) &&
                        int.TryParse(chunkIndex.StringValue, out int index))
                    {
                        allChunks.Add((index, chunk.StringValue));
                    }
                }

                nextOffset = scrollResponse.NextPageOffset;

            } while (nextOffset != null);

            return allChunks.OrderBy(c => c.ChunkIndex).Select(c => c.ChunkText).ToList();
        }
    }
}