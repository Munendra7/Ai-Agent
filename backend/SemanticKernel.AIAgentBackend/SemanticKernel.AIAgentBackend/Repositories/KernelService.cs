using Microsoft.SemanticKernel;

namespace SemanticKernel.AIAgentBackend.Repositories
{
    public class KernelService : IKernelService
    {
        private readonly IConfiguration _configuration;

        public KernelService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public Kernel GetKernel(string modelType)
        {
            var kernelBuilder = Kernel.CreateBuilder();

            if (modelType == "AzureOpenAI")
            {
                kernelBuilder.AddAzureOpenAIChatCompletion(
                    _configuration["AzureOpenAI:DeploymentName"]!,
                    _configuration["AzureOpenAI:Endpoint"]!,
                    _configuration["AzureOpenAI:ApiKey"]!
                );
            }
            else if (modelType == "Ollama")
            {
                #pragma warning disable SKEXP0070 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                kernelBuilder.AddOllamaChatCompletion(
                    modelId: _configuration["Ollama:ModelId"]!,
                    endpoint: new Uri(_configuration["Ollama:Endpoint"]!)
                );
                #pragma warning restore SKEXP0070 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            }
            else if (modelType == "OpenAI")
            {
                kernelBuilder.AddOpenAIChatCompletion(
                    _configuration["OpenAI:DeploymentName"]!,
                    _configuration["OpenAI:ApiKey"]!,
                    _configuration["OpenAI:OrgId"]!
                );
            }
            else
            {
                throw new ArgumentException("Invalid model type specified.");
            }

            return kernelBuilder.Build();
        }
    }
}
