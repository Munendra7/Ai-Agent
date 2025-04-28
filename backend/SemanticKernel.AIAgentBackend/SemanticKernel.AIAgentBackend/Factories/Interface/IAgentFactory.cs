using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;

namespace SemanticKernel.AIAgentBackend.Factories.Interface
{
    public interface IAgentFactory
    {
        ChatCompletionAgent CreateAgent(Kernel kernel, string agentName, string instructions, List<object>? plugins = null);
    }
}
