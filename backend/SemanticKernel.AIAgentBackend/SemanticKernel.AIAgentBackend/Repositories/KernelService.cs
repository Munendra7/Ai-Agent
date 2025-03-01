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
                var azureConfig = _configuration.GetSection("AzureOpenAI");
                kernelBuilder.AddAzureOpenAIChatCompletion(
                    azureConfig["DeploymentName"]!,
                    azureConfig["Endpoint"]!,
                    azureConfig["ApiKey"]!
                );
            }
            else if (modelType == "Ollama")
            {
                var ollamaConfig = _configuration.GetSection("Ollama");
                #pragma warning disable SKEXP0070 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                kernelBuilder.AddOllamaChatCompletion(
                    modelId: ollamaConfig["ModelId"]!,
                    endpoint: new Uri(ollamaConfig["Endpoint"]!)
                );
                #pragma warning restore SKEXP0070 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            }
            else if (modelType == "OpenAI")
            {
                var openAIConfig = _configuration.GetSection("OpenAI");
                kernelBuilder.AddOpenAIChatCompletion(
                    openAIConfig["DeploymentName"]!,
                    openAIConfig["ApiKey"]!,
                    openAIConfig["OrgId"]!
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
