using Microsoft.SemanticKernel;
using SemanticKernel.AIAgentBackend.Factories.Interface;

namespace SemanticKernel.AIAgentBackend.Factories.Factory
{
    public class KernelFactory : IKernelFactory
    {
        private readonly IConfiguration _configuration;

        public KernelFactory(IConfiguration configuration)
        {
            _configuration = configuration;
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

            return kernelBuilder.Build();
        }
    }
}
