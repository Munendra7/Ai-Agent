using DocxProcessorLibrary.TemplateBasedDocGenerator;
using Microsoft.SemanticKernel;
using SemanticKernel.AIAgentBackend.Factories.Interface;
using SemanticKernel.AIAgentBackend.plugins.NativePlugin;
using SemanticKernel.AIAgentBackend.Repositories.Interface;

namespace SemanticKernel.AIAgentBackend.Factories.Factory
{
    public class KernelFactory : IKernelFactory
    {
        private readonly IConfiguration _configuration;

        private readonly HttpClient _httpClient;
        private readonly IEmbeddingService _embeddingService;
        private readonly IBlobService _blobService;
        private readonly ITemplateBasedDocGenerator _templateBasedDocGenerator;

        public KernelFactory(IConfiguration configuration, HttpClient httpClient, IEmbeddingService embeddingService, IBlobService blobService, ITemplateBasedDocGenerator templateBasedDocGenerator)
        {
            _configuration = configuration;
            _httpClient = httpClient;
            _embeddingService = embeddingService;
            _blobService = blobService;
            _templateBasedDocGenerator = templateBasedDocGenerator;
        }

        public Kernel CreateKernel()
        {
            var kernelBuilder = Kernel.CreateBuilder();
            string modelType = _configuration["LLMModelType"] ?? "AzureOpenAI";
            switch (modelType)
            {
                case "AzureOpenAI":
                    kernelBuilder.AddAzureOpenAIChatCompletion(
                        _configuration["AzureOpenAI:DeploymentName"]!,
                        _configuration["AzureOpenAI:Endpoint"]!,
                        _configuration["AzureOpenAI:ApiKey"]!,
                        modelId: "gpt-4o"
                    );
                    break;
                case "Ollama":
                    #pragma warning disable SKEXP0070 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                    kernelBuilder.AddOllamaChatCompletion(
                        modelId: _configuration["Ollama:ModelId"]!,
                        endpoint: new Uri(_configuration["Ollama:Endpoint"]!)
                    );
                    #pragma warning restore SKEXP0070 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                    break;
                case "OpenAI":
                    kernelBuilder.AddOpenAIChatCompletion(
                        _configuration["OpenAI:DeploymentName"]!,
                        _configuration["OpenAI:ApiKey"]!,
                        _configuration["OpenAI:OrgId"]!
                    );
                    break;
                default:
                    throw new ArgumentException("Invalid model type specified.");
            }

            var _kernel = kernelBuilder.Build();

            var weatherPlugin = new WeatherPlugin(_kernel, _configuration);
            var googleSearchPlugin = new GoogleSearchPlugin(_kernel, _configuration);
            var ragPlugin = new RAGPlugin(_kernel, _embeddingService, _blobService, _configuration);
            var emailwriterPlugin = new EmailWriterPlugin(_httpClient, _configuration);
            var documentGenerationPlugin = new DocumentGenerationPlugin(_blobService, _templateBasedDocGenerator, _configuration);
            var excelDataAnalyzerPlugin = new ExcelDataAnalyzerPlugin(_kernel, _blobService);

            _kernel.ImportPluginFromObject(ragPlugin, "RAGPlugin");
            _kernel.ImportPluginFromObject(weatherPlugin, "WeatherPlugin");
            _kernel.ImportPluginFromObject(googleSearchPlugin, "GoogleSearchPlugin");
            _kernel.ImportPluginFromObject(emailwriterPlugin, "EmailWriterPlugin");
            _kernel.ImportPluginFromObject(documentGenerationPlugin, "DocumentGenerationPlugin");
            _kernel.ImportPluginFromObject(excelDataAnalyzerPlugin, "ExcelDataAnalyzerPlugin");

            return _kernel;
        }
    }
}
