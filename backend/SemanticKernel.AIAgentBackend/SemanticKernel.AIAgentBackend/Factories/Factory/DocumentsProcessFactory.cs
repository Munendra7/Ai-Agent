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
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Wordprocessing;
using Text = DocumentFormat.OpenXml.Wordprocessing.Text;
using Paragraph = DocumentFormat.OpenXml.Wordprocessing.Paragraph;
using Table = DocumentFormat.OpenXml.Wordprocessing.Table;
using TableCell = DocumentFormat.OpenXml.Wordprocessing.TableCell;
using TableRow = DocumentFormat.OpenXml.Wordprocessing.TableRow;
using Run = DocumentFormat.OpenXml.Wordprocessing.Run;

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

        public Dictionary<string, string> ExtractPlaceholders(Stream templateStream)
        {
            var placeholders = new Dictionary<string, string>();

            using (var memoryStream = new MemoryStream())
            {
                templateStream.CopyTo(memoryStream);
                memoryStream.Position = 0;

                using (WordprocessingDocument doc = WordprocessingDocument.Open(memoryStream, false))
                {
                    var body = doc.MainDocumentPart?.Document?.Body;
                    if (body == null)
                    {
                        throw new InvalidOperationException("Invalid or corrupted DOCX file.");
                    }

                    var regex = new Regex(@"\{\{(.*?)\}\}", RegexOptions.Compiled);

                    foreach (var paragraph in body.Descendants<Paragraph>())
                    {
                        string paragraphText = string.Join("", paragraph.Descendants<Text>().Select(t => t.Text));
                        ExtractMatches(regex, paragraphText, placeholders);
                    }

                    foreach (var table in body.Descendants<Table>())
                    {
                        foreach (var cell in table.Descendants<TableCell>())
                        {
                            string cellText = string.Join("", cell.Descendants<Text>().Select(t => t.Text));
                            ExtractMatches(regex, cellText, placeholders, isTable: true);
                        }
                    }

                    foreach (var sdt in body.Descendants<SdtElement>())
                    {
                        string sdtText = string.Join("", sdt.Descendants<Text>().Select(t => t.Text));
                        ExtractMatches(regex, sdtText, placeholders);
                    }
                }
            }

            return placeholders;
        }

        // Extracts and stores placeholders in the dictionary
        private void ExtractMatches(Regex regex, string text, Dictionary<string, string> placeholders, bool isTable = false)
        {
            if (!string.IsNullOrWhiteSpace(text))
            {
                Console.WriteLine($"Checking text: {text}"); // Debugging

                foreach (Match match in regex.Matches(text))
                {
                    string key = match.Groups[1].Value;
                    placeholders[key] = isTable || key.StartsWith("Table:") ? "table" : "text";
                }
            }
        }

        public MemoryStream ReplacePlaceholdersInDocx(Stream templateStream, Dictionary<string, string>? parameters, Dictionary<string, List<List<string>>>? tableInputs)
        {
            var outputStream = new MemoryStream();

            // Ensure the template stream is seekable before resetting the position
            if (templateStream.CanSeek)
            {
                templateStream.Position = 0;
            }

            templateStream.CopyTo(outputStream);
            outputStream.Position = 0; // Reset after copying

            using (WordprocessingDocument doc = WordprocessingDocument.Open(outputStream, true))
            {
                var body = doc.MainDocumentPart?.Document.Body;
                if (body == null)
                {
                    return outputStream;
                }

                if(parameters != null)
                {
                    foreach (var param in parameters)
                    {
                        foreach (var text in body.Descendants<Text>())
                        {
                            if (!string.IsNullOrEmpty(text.Text) && text.Text.Contains($"{{{{{param.Key}}}}}"))
                            {
                                text.Text = text.Text.Replace($"{{{{{param.Key}}}}}", param.Value);
                            }
                        }
                    }
                }
               

                if(tableInputs != null)
                {
                    foreach (var tableKey in tableInputs.Keys)
                    {
                        var table = body.Descendants<Table>()
                            .FirstOrDefault(t => t.Descendants<TableCell>().Any(tc => tc.InnerText.Contains($"{{{{{tableKey}}}}}")));

                        if (table != null)
                        {
                            // Find the row that contains the placeholder
                            var placeholderRow = table.Elements<TableRow>()
                                .FirstOrDefault(tr => tr.InnerText.Contains($"{{{{{tableKey}}}}}"));

                            if (placeholderRow != null)
                            {
                                // Clone the placeholder row structure for new rows (without placeholder text)
                                var newRowTemplate = (TableRow)placeholderRow.CloneNode(true);

                                // Remove the placeholder row
                                table.RemoveChild(placeholderRow);

                                // ✅ Insert new rows dynamically
                                foreach (var rowData in tableInputs[tableKey])
                                {
                                    var newRow = (TableRow)newRowTemplate.CloneNode(true); // Clone for structure
                                    var cells = newRow.Elements<TableCell>().ToList();

                                    for (int i = 0; i < rowData.Count && i < cells.Count; i++)
                                    {
                                        var cellText = cells[i].Descendants<Text>().FirstOrDefault();
                                        if (cellText != null)
                                        {
                                            cellText.Text = rowData[i]; // Replace text inside cloned cell
                                        }
                                    }
                                    table.AppendChild(newRow);
                                }
                            }
                        }
                    }
                }

                doc?.MainDocumentPart?.Document.Save();
            }

            outputStream.Position = 0; // Ensure stream is at the start for reading
            return outputStream;
        }
    }
}
