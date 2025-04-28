using ExcelDataReader;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Data.Analysis;
using Microsoft.SemanticKernel;
using Newtonsoft.Json;
using SemanticKernel.AIAgentBackend.Repositories.Interface;
using System.ComponentModel;
using System.Data;
using static UglyToad.PdfPig.Writer.PdfPageBuilder;

namespace SemanticKernel.AIAgentBackend.plugins.NativePlugin
{
    public class Globals
    {
        public DataFrame? df;
    }

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

        [KernelFunction("LoadExcel"), Description("Load an Excel file of xlsx format into memory")]
        public async Task<string> LoadExcelAsync(string filename)
        {
            var (contentStream, url) = await _blobService.DownloadFileAsync(filename, Constants.BlobStorageConstants.KnowledgeContainerName);

            using var memoryStream = new MemoryStream();
            await contentStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            using var reader = ExcelReaderFactory.CreateReader(memoryStream);

            var ds = reader.AsDataSet(new ExcelDataSetConfiguration
            {
                ConfigureDataTable = (_) => new ExcelDataTableConfiguration
                {
                    UseHeaderRow = true
                }
            });

            var table = ds.Tables[0];

            // Convert DataTable to DataFrame
            _df = DataTableToDataFrame(table);

            return $"Loaded data: {_df.Rows.Count} rows, {_df.Columns.Count} columns.";
        }

        private DataFrame DataTableToDataFrame(DataTable table)
        {
            var df = new DataFrame();
            foreach (DataColumn col in table.Columns)
            {
                Type dataType = col.DataType;

                if (dataType == typeof(int) || dataType == typeof(long))
                {
                    var column = new Int64DataFrameColumn(col.ColumnName, table.Rows.Count);
                    for (int i = 0; i < table.Rows.Count; i++)
                    {
                        var val = table.Rows[i][col];
                        column[i] = val == DBNull.Value ? null : (long?)Convert.ToInt64(val);
                    }
                    df.Columns.Add(column);
                }
                else if (dataType == typeof(float) || dataType == typeof(double) || dataType == typeof(decimal))
                {
                    var column = new DoubleDataFrameColumn(col.ColumnName, table.Rows.Count);
                    for (int i = 0; i < table.Rows.Count; i++)
                    {
                        var val = table.Rows[i][col];
                        column[i] = val == DBNull.Value ? null : (double?)Convert.ToDouble(val);
                    }
                    df.Columns.Add(column);
                }
                else
                {
                    // Fallback for text columns
                    var column = new StringDataFrameColumn(col.ColumnName, table.Rows.Count);
                    for (int i = 0; i < table.Rows.Count; i++)
                    {
                        var val = table.Rows[i][col];
                        column[i] = val == DBNull.Value ? null : val.ToString();
                    }
                    df.Columns.Add(column);
                }
            }
            return df;
        }



        [KernelFunction("QueryData"), Description("Query loaded Excel data using natural language.")]
        public async Task<string> QueryDataAsync(string query)
        {
            if (_df == null)
            {
                return "Error: No data loaded. Please call LoadExcel first.";
            }

            // List column names dynamically
            var cols = _df.Columns.Select(c => c.Name).ToArray();
            var colsList = string.Join(", ", cols);

            var prompt = $@"You have a Microsoft.Data.Analysis.DataFrame named 'df' with columns: [{colsList}].
            Write C# code using Microsoft.Data.Analysis to answer the question: {query}.
            Write only the body of C# code (no `using` statements, no namespaces, no classes).
            Assign the final result to a variable named 'result'.
            Return only the C# code, no explanation.";

            // Generate C# code via LLM
            var runResult = await _kernel.InvokePromptAsync(prompt);

            var code = runResult.GetValue<string>()?.Trim();

            var cleanedCode = string.Join(Environment.NewLine,
                (runResult.GetValue<string>() ?? "")
                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .SkipWhile(line => !line.TrimStart().StartsWith("```"))
                    .Skip(1)
                    .TakeWhile(line => !line.TrimStart().StartsWith("```"))
                    .Where(line => !line.TrimStart().StartsWith("using "))
            );

            // Prepare scripting options
            var scriptOptions = ScriptOptions.Default
                .AddReferences(typeof(DataFrame).Assembly, typeof(Enumerable).Assembly)
                .AddImports("System", "System.Linq", "Microsoft.Data.Analysis");

            // Execute generated code with globals
            var globals = new Globals { df = _df };
            try
            {
                var script = CSharpScript.Create(cleanedCode, scriptOptions, typeof(Globals));
                var state = await script.RunAsync(globals);
                var result = state.Variables.FirstOrDefault(v => v.Name == "result")?.Value;

                // Serialize result to JSON
                return JsonConvert.SerializeObject(result);
            }
            catch (Exception ex)
            {
                return $"Code execution error: {ex.Message}";
            }
        }
    }
}
