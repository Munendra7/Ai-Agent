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

        public async Task<IActionResult> ProcessFileAsync(FileUploadDTO fileDTO)
        {
            if (fileDTO == null || fileDTO.File.Length == 0)
            {
                return new BadRequestObjectResult("Invalid file.");
            }

            List<string> textChunks = _documentsProcessFactory.ExtractTextChunksFromFile(fileDTO.File).ToList();
            if (textChunks.Count == 0)
            {
                return new BadRequestObjectResult("Could not extract text from the file.");
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
            await StoreEmbeddingAsync(fileDTO.FileName, fileDTO.FileDescription, embeddings, chunkTexts);

            return new OkObjectResult("File processed and embeddings stored successfully.");
        }

        private async Task StoreEmbeddingAsync(string fileName, string? fileDescription, List<float[]> embeddings, List<string> chunkTexts)
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
                limit: 5
            );

            return returnedLocations;
        }

        public async Task<List<string>> GetAllDocumentsAsync()
        {
            var collectionName = _configuration["Qdrant:CollectionName"] ?? "document_embeddings";
            var scrollPoints = new ScrollPoints
            {
                CollectionName = collectionName,
                WithPayload = new WithPayloadSelector { Enable = true },
                Limit = 10 // Adjust based on expected number of documents
            };

            var scrollResponse = await _qdrantClient.ScrollAsync(collectionName, filter: null, limit: scrollPoints.Limit, offset: scrollPoints.Offset, payloadSelector: scrollPoints.WithPayload);

            return scrollResponse.Result
                .Select(p => p.Payload["FileName"].StringValue)
                .Distinct()
                .ToList();
        }

        public async Task<List<string>> RetrieveDocumentChunksAsync(string documentName)
        {
            var collectionName = _configuration["Qdrant:CollectionName"] ?? "document_embeddings";
            var scrollPoints = new ScrollPoints
            {
                CollectionName = collectionName,
                WithPayload = new WithPayloadSelector { Enable = true },
                Limit = 10
            };

            var scrollResponse = await _qdrantClient.ScrollAsync(collectionName, filter: null, limit: scrollPoints.Limit, offset: scrollPoints.Offset, payloadSelector: scrollPoints.WithPayload);

            var documentChunks = scrollResponse.Result
                .Where(p => p.Payload["FileName"].StringValue == documentName)
                .OrderBy(p => int.Parse(p.Payload["ChunkIndex"].StringValue))
                .Select(p => p.Payload["Chunk"].StringValue)
                .ToList();

            return documentChunks;
        }

    }
}