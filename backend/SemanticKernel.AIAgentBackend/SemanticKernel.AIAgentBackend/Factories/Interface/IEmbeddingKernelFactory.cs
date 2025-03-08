using Microsoft.SemanticKernel;

namespace SemanticKernel.AIAgentBackend.Factories.Interface
{
    public interface IEmbeddingKernelFactory
    {
        /// <summary>
        /// Creates a new instance of the <see cref="Kernel"/> class.
        Kernel CreateKernel();
    }
}
