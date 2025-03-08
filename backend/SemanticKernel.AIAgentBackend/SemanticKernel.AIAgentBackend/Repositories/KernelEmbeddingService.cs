using Microsoft.SemanticKernel;

namespace SemanticKernel.AIAgentBackend.Repositories
{
    public class KernelEmbeddingService : IKernelEmbeddingService
    {
        private readonly IConfiguration _configuration;

        public KernelEmbeddingService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public Kernel GetKernel(string modelType)
        {
            var kernelBuilder = Kernel.CreateBuilder();

            if (modelType == "AzureOpenAI")
            {
                #pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                kernelBuilder.AddAzureOpenAITextEmbeddingGeneration(
                    deploymentName: _configuration["AzureOpenAI:EmbeddingDeployment"]!,
                    endpoint: _configuration["AzureOpenAI:Endpoint"]!,
                    apiKey: _configuration["AzureOpenAI:ApiKey"]!
                );
                #pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            }
            else if (modelType == "Ollama")
            {
                #pragma warning disable SKEXP0070
                kernelBuilder.AddOllamaTextEmbeddingGeneration(
                    modelId: _configuration["Ollama:EmbeddingModelId"]!,
                    endpoint: new Uri(_configuration["Ollama:Endpoint"]!)
                );
                #pragma warning restore SKEXP0070
            }
            else if (modelType == "OpenAI")
            {
                #pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                kernelBuilder.AddOpenAITextEmbeddingGeneration(
                    modelId: _configuration["OpenAI:EmbeddingModelId"]!,
                    apiKey: _configuration["OpenAI:ApiKey"]!,
                    orgId: _configuration["OpenAI:OrgId"]
                );
                #pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            }
            else
            {
                throw new ArgumentException("Invalid model type specified.");
            }

            return kernelBuilder.Build();
        }
    }
}
