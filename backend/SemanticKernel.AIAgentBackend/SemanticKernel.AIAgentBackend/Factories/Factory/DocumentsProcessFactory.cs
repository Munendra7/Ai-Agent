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
using TableProperties = DocumentFormat.OpenXml.Wordprocessing.TableProperties;
using TopBorder = DocumentFormat.OpenXml.Wordprocessing.TopBorder;
using BottomBorder = DocumentFormat.OpenXml.Wordprocessing.BottomBorder;
using LeftBorder = DocumentFormat.OpenXml.Wordprocessing.LeftBorder;
using RightBorder = DocumentFormat.OpenXml.Wordprocessing.RightBorder;
using InsideHorizontalBorder = DocumentFormat.OpenXml.Wordprocessing.InsideHorizontalBorder;
using InsideVerticalBorder = DocumentFormat.OpenXml.Wordprocessing.InsideVerticalBorder;
using DocumentFormat.OpenXml;
using System.Text.Json;
using ParagraphProperties = DocumentFormat.OpenXml.Wordprocessing.ParagraphProperties;
using TableCellProperties = DocumentFormat.OpenXml.Wordprocessing.TableCellProperties;
using RunProperties = DocumentFormat.OpenXml.Wordprocessing.RunProperties;

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

        public HashSet<string> ExtractPlaceholders(Stream templateStream)
        {
            var placeholders = new HashSet<string>();

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

                    foreach (var element in body.Descendants<OpenXmlElement>())
                    {
                        string elementText = GetFullText(element);
                        ExtractMatches(regex, elementText, placeholders);
                    }
                }
            }

            return placeholders;
        }

        private void ExtractMatches(Regex regex, string text, HashSet<string> placeholders)
        {
            foreach (Match match in regex.Matches(text))
            {
                placeholders.Add(match.Groups[1].Value.Trim()); // Store only the placeholder name
            }
        }

        private string GetFullText(OpenXmlElement element)
        {
            return string.Join("", element.Descendants<Run>().Select(r => r.InnerText));
        }

        public MemoryStream ReplacePlaceholdersInDocx(Stream templateStream, Dictionary<string, object> dynamicInputs)
        {
            var outputStream = new MemoryStream();

            if (templateStream.CanSeek)
            {
                templateStream.Position = 0;
            }

            templateStream.CopyTo(outputStream);
            outputStream.Position = 0;

            using (WordprocessingDocument doc = WordprocessingDocument.Open(outputStream, true))
            {
                var body = doc.MainDocumentPart?.Document.Body;
                if (body == null) return outputStream;

                foreach (var key in dynamicInputs.Keys)
                {
                    var value = dynamicInputs[key];

                    if (value is JsonElement jsonElement)
                    {
                        if (jsonElement.ValueKind == JsonValueKind.String)
                        {
                            value = jsonElement.GetString();
                        }
                        else if (jsonElement.ValueKind == JsonValueKind.Array)
                        {
                            value = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(jsonElement.GetRawText());
                        }
                        else
                        {
                            value = jsonElement.ToString();
                        }
                    }

                    if (value is string textValue)
                    {
                        foreach (var text in body.Descendants<Text>())
                        {
                            if (!string.IsNullOrEmpty(text.Text) && text.Text.Contains($"{{{{{key}}}}}"))
                            {
                                text.Text = text.Text.Replace($"{{{{{key}}}}}", textValue);
                            }
                        }
                    }

                    else if (value is List<Dictionary<string, string>> tableData && tableData.Count > 0)
                    {
                        var headers = tableData[0].Keys.ToList();

                        var placeholderParagraph = body.Descendants<Paragraph>()
                            .FirstOrDefault(p => p.InnerText.Contains($"{{{{{key}}}}}"));

                        if (placeholderParagraph != null)
                        {
                            var parentElement = placeholderParagraph.Parent;
                            var nextSibling = placeholderParagraph.NextSibling();

                            parentElement?.RemoveChild(placeholderParagraph);

                            var newTable = new Table(
                                new TableProperties(
                                    new TableWidth { Width = "100%", Type = TableWidthUnitValues.Pct },
                                    new TableBorders(
                                        new TopBorder { Val = BorderValues.Single, Size = 12 },
                                        new BottomBorder { Val = BorderValues.Single, Size = 12 },
                                        new LeftBorder { Val = BorderValues.Single, Size = 12 },
                                        new RightBorder { Val = BorderValues.Single, Size = 12 },
                                        new InsideHorizontalBorder { Val = BorderValues.Single, Size = 12 },
                                        new InsideVerticalBorder { Val = BorderValues.Single, Size = 12 }
                                    )
                                )
                            );

                            var headerRow = new TableRow();
                            foreach (var header in headers)
                            {
                                var headerCell = new TableCell(
                                    new TableCellProperties(new Shading { Fill = "BFBFBF", Val = ShadingPatternValues.Clear }),
                                    new Paragraph(
                                        new ParagraphProperties(new Justification { Val = JustificationValues.Center }),
                                        new Run(new RunProperties(new Bold()), new Text(header))
                                    )
                                );
                                headerRow.Append(headerCell);
                            }
                            newTable.Append(headerRow);

                            foreach (var rowData in tableData)
                            {
                                var newRow = new TableRow();
                                foreach (var header in headers)
                                {
                                    var cellValue = rowData.ContainsKey(header) ? rowData[header] : "";
                                    var newCell = new TableCell(
                                        new Paragraph(
                                            new ParagraphProperties(new Justification { Val = JustificationValues.Left }),
                                            new Run(new Text(cellValue))
                                        )
                                    );
                                    newRow.Append(newCell);
                                }
                                newTable.Append(newRow);
                            }

                            if (nextSibling != null)
                            {
                                parentElement?.InsertBefore(newTable, nextSibling);
                            }
                            else
                            {
                                parentElement?.AppendChild(newTable);
                            }
                        }
                    }
                }

                doc.MainDocumentPart?.Document.Save();
            }

            outputStream.Position = 0;
            return outputStream;
        }
    }
}
