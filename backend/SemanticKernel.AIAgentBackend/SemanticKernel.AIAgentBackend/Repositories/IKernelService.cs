using Microsoft.SemanticKernel;

namespace SemanticKernel.AIAgentBackend.Repositories
{
    public interface IKernelService
    {
        Kernel GetKernel(string modelType);
    }
}
