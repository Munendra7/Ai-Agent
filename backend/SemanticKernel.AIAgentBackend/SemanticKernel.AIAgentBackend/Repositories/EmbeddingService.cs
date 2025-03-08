using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.DataFormats;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using SemanticKernel.AIAgentBackend.Models.DTO;
using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace SemanticKernel.AIAgentBackend.Repositories
{
    public class EmbeddingService : IEmbeddingService
    {
        private readonly IKernelEmbeddingService _kernel;
        private readonly QdrantClient _qdrantClient;
        private const string CollectionName = "document_embeddings";

        public EmbeddingService(IKernelEmbeddingService kernel, QdrantClient qdrantClient)
        {
            _kernel = kernel;
            _qdrantClient = qdrantClient;
        }

        public async Task<IActionResult> ProcessFileAsync(FileUploadDTO fileDTO)
        {
            if (fileDTO == null || fileDTO.File.Length == 0)
            {
                return new BadRequestObjectResult("Invalid file.");
            }

            List<string> textChunks = ExtractTextChunksFromFile(fileDTO.File).ToList();
            if (textChunks.Count == 0)
            {
                return new BadRequestObjectResult("Could not extract text from the file.");
            }

            // Generate embeddings for each chunk
            var embeddings = new List<float[]>();
            var chunkTexts = new List<string>();
            var kernel = _kernel.GetKernel(fileDTO.Model);
            #pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            var _embeddingGenerator = kernel.Services.GetRequiredService<ITextEmbeddingGenerationService>();
            #pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
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

        private IEnumerable<string> ExtractTextChunksFromFile(IFormFile file, int chunkSize = 512)
        {
            using var stream = file.OpenReadStream();
            if (file.FileName.EndsWith(".txt"))
            {
                return ExtractTextChunksFromTxt(stream, chunkSize);
            }
            else if (file.FileName.EndsWith(".pdf"))
            {
                return ExtractTextChunksFromPdf(stream, chunkSize);
            }
            else if (file.FileName.EndsWith(".docx"))
            {
                return ExtractTextChunksFromDocx(stream, chunkSize);
            }
            return Enumerable.Empty<string>();
        }

        private IEnumerable<string> ExtractTextChunksFromTxt(Stream stream, int chunkSize)
        {
            using var reader = new StreamReader(stream);
            return ChunkText(reader.ReadToEnd(), chunkSize);
        }

        private IEnumerable<string> ExtractTextChunksFromPdf(Stream stream, int chunkSize)
        {
            using var pdfDocument = PdfDocument.Open(stream);
            var text = new StringBuilder();
            foreach (var page in pdfDocument.GetPages())
            {
                text.Append(ContentOrderTextExtractor.GetText(page));
            }
            return ChunkText(text.ToString(), chunkSize);
        }

        private IEnumerable<string> ExtractTextChunksFromDocx(Stream stream, int chunkSize)
        {
            StringBuilder text = new StringBuilder();
            using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(stream, false))
            {
                var body = wordDoc.MainDocumentPart?.Document.Body;
                if (body != null)
                {
                    foreach (var para in body.Elements<Paragraph>())
                    {
                        text.AppendLine(para.InnerText);
                    }
                }
            }
            return ChunkText(text.ToString(), chunkSize);
        }

        private IEnumerable<string> ChunkText(string text, int chunkSize)
        {
            List<string> chunks = new();
            StringBuilder currentChunk = new();
            foreach (var word in text.Split(' '))
            {
                if (currentChunk.Length + word.Length > chunkSize)
                {
                    chunks.Add(currentChunk.ToString());
                    currentChunk.Clear();
                }
                currentChunk.Append(word).Append(" ");
            }
            if (currentChunk.Length > 0)
            {
                chunks.Add(currentChunk.ToString());
            }
            return chunks;
        }

        private async Task StoreEmbeddingAsync(string fileName, string? fileDescription, List<float[]> embeddings, List<string> chunkTexts)
        {
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
            if (!existingCollections.Any(name => name == CollectionName))
            {
                await _qdrantClient.CreateCollectionAsync(CollectionName, new VectorParams
                {
                    Size = 768,
                    Distance = Distance.Cosine
                });
            }
            await _qdrantClient.UpsertAsync(CollectionName, points);
        }

        public async Task<IReadOnlyList<ScoredPoint>> SimilaritySearch(string prompt)
        {
            var kernel = _kernel.GetKernel("Ollama");
            #pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            var _embeddingGenerator = kernel.Services.GetRequiredService<ITextEmbeddingGenerationService>();
            #pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.var _embeddingGenerator = kernel.Services.GetRequiredService<ITextEmbeddingGenerationService>();

            var promptEmbedding = await _embeddingGenerator.GenerateEmbeddingAsync(prompt);

            var returnedLocations = await _qdrantClient.QueryAsync(
                collectionName: CollectionName,
                query: promptEmbedding.ToArray(),
                limit: 5
            );

            return returnedLocations;
        }
    }
}