using DocumentFormat.OpenXml.Packaging;
using System.Text;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using UglyToad.PdfPig;
using SemanticKernel.AIAgentBackend.Factories.Interface;
using DocumentFormat.OpenXml.Drawing;

namespace SemanticKernel.AIAgentBackend.Factories.Factory
{
    public class DocumentsProcessFactory : IDocumentsProcessFactory
    {
        public DocumentsProcessFactory() { }

        public IEnumerable<string> ExtractTextChunksFromFile(IFormFile file, int chunkSize = 512)
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
    }
}
