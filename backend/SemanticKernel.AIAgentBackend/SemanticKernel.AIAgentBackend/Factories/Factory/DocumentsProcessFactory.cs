using DocumentFormat.OpenXml.Packaging;
using System.Text;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using UglyToad.PdfPig;
using SemanticKernel.AIAgentBackend.Factories.Interface;
using DocumentFormat.OpenXml.Drawing;
using System.Formats.Asn1;
using System.Globalization;
using OfficeOpenXml;
using CsvHelper;

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
            else if (file.FileName.EndsWith(".xlsx") || file.FileName.EndsWith(".xls"))
            {
                return ExtractStructuredDataFromExcel(stream);
            }
            else if (file.FileName.EndsWith(".csv"))
            {
                return ExtractStructuredDataFromCsv(stream);
            }
            return Enumerable.Empty<string>();
        }

        private IEnumerable<string> ExtractStructuredDataFromExcel(Stream stream)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage(stream);
            var structuredData = new List<string>();
            foreach (var worksheet in package.Workbook.Worksheets)
            {
                int rowCount = worksheet.Dimension?.Rows ?? 0;
                int colCount = worksheet.Dimension?.Columns ?? 0;
                for (int row = 2; row <= rowCount; row++)
                {
                    var rowData = new List<string>();
                    for (int col = 1; col <= colCount; col++)
                    {
                        rowData.Add($"{worksheet.Cells[1, col].Text}:{worksheet.Cells[row, col].Text}");
                    }
                    structuredData.Add(string.Join(" | ", rowData));
                }
            }
            return structuredData;
        }

        private IEnumerable<string> ExtractStructuredDataFromCsv(Stream stream)
        {
            using var reader = new StreamReader(stream);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            var structuredData = new List<string>();

            if (!csv.Read() || !csv.ReadHeader())
                return structuredData;

            var headers = csv.HeaderRecord;

            while (csv.Read())
            {
                var rowData = new List<string>();
                for (int i = 0; i < headers?.Length; i++)
                {
                    rowData.Add($"{headers[i]}:{csv.GetField(i)}");
                }
                structuredData.Add(string.Join(" | ", rowData));
            }

            return structuredData;
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
