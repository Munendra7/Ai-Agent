using Microsoft.SemanticKernel;

namespace SemanticKernel.AIAgentBackend.Repositories
{
    public interface IKernelEmbeddingService
    {
        Kernel GetKernel(string modelType);
    }
}
