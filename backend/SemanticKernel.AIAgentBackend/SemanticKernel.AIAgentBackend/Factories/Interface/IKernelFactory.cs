using Microsoft.SemanticKernel;

namespace SemanticKernel.AIAgentBackend.Factories.Interface
{
    public interface IKernelFactory
    {
        /// <summary>
        /// Creates a new instance of the <see cref="Kernel"/> class.
        Kernel CreateKernel();
    }
}
