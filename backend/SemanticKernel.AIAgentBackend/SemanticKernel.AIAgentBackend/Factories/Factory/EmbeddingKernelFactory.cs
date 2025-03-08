using Microsoft.SemanticKernel;
using SemanticKernel.AIAgentBackend.Factories.Interface;

namespace SemanticKernel.AIAgentBackend.Factories.Factory
{
    public class EmbeddingKernelFactory : IEmbeddingKernelFactory
    {
        private readonly IConfiguration _configuration;

        public EmbeddingKernelFactory(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public Kernel CreateKernel()
        {
            var kernelBuilder = Kernel.CreateBuilder();
            string modelType = _configuration["EmbeddingModelType"] ?? "Ollama";
            switch (modelType)
            {
                case "AzureOpenAI":
                    #pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                    kernelBuilder.AddAzureOpenAITextEmbeddingGeneration(
                        deploymentName: _configuration["AzureOpenAI:EmbeddingDeployment"]!,
                        endpoint: _configuration["AzureOpenAI:Endpoint"]!,
                        apiKey: _configuration["AzureOpenAI:ApiKey"]!
                    );
                    #pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                    break;

                case "Ollama":
                    #pragma warning disable SKEXP0070 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                    kernelBuilder.AddOllamaTextEmbeddingGeneration(
                        modelId: _configuration["Ollama:EmbeddingModelId"]!,
                        endpoint: new Uri(_configuration["Ollama:Endpoint"]!)
                    );
                    #pragma warning restore SKEXP0070 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                    break;

                case "OpenAI":
                    #pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                    kernelBuilder.AddOpenAITextEmbeddingGeneration(
                        modelId: _configuration["OpenAI:EmbeddingModelId"]!,
                        apiKey: _configuration["OpenAI:ApiKey"]!,
                        orgId: _configuration["OpenAI:OrgId"]
                    );
                    #pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                    break;

                default:
                    throw new ArgumentException("Invalid model type specified.");
            }

            return kernelBuilder.Build();
        }
    }
}