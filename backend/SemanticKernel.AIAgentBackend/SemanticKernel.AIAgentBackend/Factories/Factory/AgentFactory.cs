using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel;
using SemanticKernel.AIAgentBackend.Factories.Interface;

namespace SemanticKernel.AIAgentBackend.Factories.Factory
{
    public class AgentFactory : IAgentFactory
    {
        // Method to create an agent with flexible plugin support
        public ChatCompletionAgent CreateAgent(Kernel kernel, string agentName, string instructions, List<object>? plugins = null)
        {
            var agentKernel = kernel.Clone();

            // Dynamically import each plugin into the kernel
            if (plugins != null)
            {
                foreach (var plugin in plugins)
                {
                    if (plugin != null)
                    {
                        agentKernel.ImportPluginFromObject(plugin, plugin.GetType().Name);
                    }
                }
            }

            return new ChatCompletionAgent
            {
                Name = agentName,
                Instructions = instructions,
                Kernel = agentKernel,
                Arguments = new KernelArguments(
                    new OpenAIPromptExecutionSettings
                    {
                        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
                        MaxTokens = 1000,
                        Temperature = 0.2,
                        TopP = 0.5,
                        ChatSystemPrompt = instructions
                    })
            };
        }
    }
}
