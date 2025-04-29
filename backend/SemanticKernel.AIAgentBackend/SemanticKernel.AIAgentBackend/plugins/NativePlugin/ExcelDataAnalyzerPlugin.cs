using System;
using System.IO;
using System.Linq;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using ExcelDataReader;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Data.Analysis;
using Microsoft.SemanticKernel;
using Newtonsoft.Json;
using SemanticKernel.AIAgentBackend.Repositories.Interface;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using System.ComponentModel;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Office2013.Drawing.ChartStyle;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using DocumentFormat.OpenXml.Office2016.Excel;
using Microsoft.AspNetCore.Http.HttpResults;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Math;
using Path = System.IO.Path;

namespace SemanticKernel.AIAgentBackend.plugins.NativePlugin
{
    public class Globals
    {
        public required DataFrame df { get; set; }
    }

    /// <summary>
    /// Helper to load an Excel stream into a DataFrame.
    /// </summary>
    public static class DataFrameExcelLoader
    {
        public static DataFrame LoadFromExcel(Stream excelStream, string sheetName)
        {
            System.Text.Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            using var reader = ExcelReaderFactory.CreateReader(excelStream);
            var config = new ExcelDataSetConfiguration
            {
                ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = true }
            };
            var ds = reader.AsDataSet(config);
            var tableToUse = (System.Data.DataTable?)null; // Explicitly specify the type to resolve CS0815  
            if (!string.IsNullOrWhiteSpace(sheetName) && ds.Tables.Contains(sheetName))
            {
                tableToUse = ds.Tables[sheetName];
            }
            else
            {
                tableToUse = ds.Tables[0];
            }
            return ConvertTable(tableToUse);
        }

        private static DataFrame ConvertTable(System.Data.DataTable? table)
        {
            if(table == null)
                throw new ArgumentNullException(nameof(table), "DataTable cannot be null.");

            // Sanitize column names
            foreach (DataColumn col in table.Columns)
            {
                col.ColumnName = col.ColumnName.Replace("\r", "").Replace("\n", "").Trim();
            }

            var columns = table.Columns.Cast<DataColumn>()
                .Select(col => CreateColumn(col, table.Rows.Cast<DataRow>()))
                .ToArray();
            return new DataFrame(columns);
        }

        private static DataFrameColumn CreateColumn(DataColumn col, IEnumerable<DataRow> rows)
        {
            var name = col.ColumnName;
            var data = rows.Select(r => r[col] is DBNull ? null : r[col]);

            return col.DataType switch
            {
                Type t when t == typeof(bool) =>
                    new BooleanDataFrameColumn(name, data.Cast<bool?>().ToArray()),

                Type t when t == typeof(int) || t == typeof(long) =>
                    new Int64DataFrameColumn(name, data.Cast<long?>().ToArray()),

                Type t when t == typeof(float) || t == typeof(double) || t == typeof(decimal) =>
                    new DoubleDataFrameColumn(name, data.Cast<double?>().ToArray()),

                Type t when t == typeof(DateTime) =>
                    new PrimitiveDataFrameColumn<DateTime>(name, data.Cast<DateTime?>().ToArray()),

                _ =>
                    new StringDataFrameColumn(name, data.Cast<string>().ToArray())
            };
        }
    }

    /// <summary>
    /// Plugin for loading Excel data and querying it via Semantic Kernel.
    /// </summary>
    public class ExcelDataAnalyzerPlugin
    {
        private DataFrame _df = new DataFrame();
        private readonly Kernel _kernel;
        private readonly IBlobService _blobService;

        public ExcelDataAnalyzerPlugin([FromKeyedServices("LLMKernel")] Kernel kernel, IBlobService blobService)
        {
            _kernel = kernel;
            _blobService = blobService;
        }

        [KernelFunction("LoadExcel"), Description("Load an Excel xlsx file into a DataFrame from given sheet name")]
        public async Task<string> LoadExcelAsync(string filename, [Description("Excel Sheet from which data will be loaded")] string sheetName)
        {
            var (contentStream, _) = await _blobService.DownloadFileAsync(
                filename,
                Constants.BlobStorageConstants.KnowledgeContainerName);

            using var ms = new MemoryStream();
            await contentStream.CopyToAsync(ms);
            ms.Position = 0;

            _df = DataFrameExcelLoader.LoadFromExcel(ms, sheetName);
            return $"Loaded data: {_df.Rows.Count} rows, {_df.Columns.Count} columns.";
        }

        private static string GetTypeName(DataFrameColumn col) => col switch
        {
            BooleanDataFrameColumn => "bool",
            Int64DataFrameColumn => "long",
            DoubleDataFrameColumn => "double",
            PrimitiveDataFrameColumn<DateTime> => "DateTime",
            StringDataFrameColumn => "string",
            _ => "object"
        };

        [KernelFunction("QueryData"), Description("Query loaded Excel data using natural language.")]
        public async Task<string> QueryDataAsync(string query)
        {
            if (_df == null || _df.Rows.Count == 0)
                return "Error: No data loaded. Please call LoadExcel first.";

            string basePath = AppContext.BaseDirectory;
            string pluginPath = Path.Combine(basePath, "plugins", "CodeWriterPlugin");

            var plugin_CodeWriter = _kernel.ImportPluginFromPromptDirectory(pluginPath, "CodeWriterPlugin");

            var desc = string.Join(", ", _df.Columns.Select(c => $"{c.Name} ({GetTypeName(c)})"));

            var dataAnalysisExpert = plugin_CodeWriter["DataAnalysisExpert"];

            string raw_code = (await _kernel.InvokeAsync(dataAnalysisExpert, new KernelArguments()
            {
                ["query"] = query,
                ["columnsdesc"] = desc
            })).ToString();

            var lines = raw_code.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var codeBody = string.Join("\n",
                lines.SkipWhile(l => !l.TrimStart().StartsWith("```")).Skip(1)
                     .TakeWhile(l => !l.TrimStart().StartsWith("```")).Select(l =>
                        l.TrimStart().StartsWith("using ") ? string.Empty : l)
                     .Where(l => !string.IsNullOrWhiteSpace(l)));
            var code = string.IsNullOrWhiteSpace(codeBody) ? raw_code : codeBody;

            var options = ScriptOptions.Default
                .AddReferences(typeof(DataFrame).Assembly)
                .AddImports("System", "System.Linq", "Microsoft.Data.Analysis");

            // Retry loop: execute & let LLM fix code on failures
            const int MAX_EXEC_TRIES = 7;
            var currentCode = code;
            object? executionResult = null;

            for (int attempt = 1; attempt <= MAX_EXEC_TRIES; attempt++)
            {
                try
                {
                    var script = CSharpScript.Create(currentCode, options, typeof(Globals));
                    var state = await script.RunAsync(new Globals { df = _df });
                    executionResult = state.Variables.FirstOrDefault(v => v.Name == "result")?.Value;
                    break;
                }
                catch (Exception ex) when (attempt < MAX_EXEC_TRIES)
                {
                    var codeFixerExpert = plugin_CodeWriter["CodeFixer"];

                    string fixed_raw_code = (await _kernel.InvokeAsync(dataAnalysisExpert, new KernelArguments()
                    {
                        ["currentCode"] = currentCode,
                        ["error"] = ex.Message
                    })).ToString();

                    var fixLines = fixed_raw_code.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    var fixBody = fixLines
                        .SkipWhile(l => !l.TrimStart().StartsWith("```")).Skip(1)
                        .TakeWhile(l => !l.TrimStart().StartsWith("```"))
                        .Where(l => !l.TrimStart().StartsWith("using "))
                        .ToArray();
                    currentCode = fixBody.Length > 0 ? string.Join("\n", fixBody) : fixed_raw_code;
                }
                catch (Exception ex)
                {
                    return $"Code execution failed after {MAX_EXEC_TRIES} attempts: {ex.Message}";
                }
            }

            return JsonConvert.SerializeObject(executionResult);
        }
    }
}