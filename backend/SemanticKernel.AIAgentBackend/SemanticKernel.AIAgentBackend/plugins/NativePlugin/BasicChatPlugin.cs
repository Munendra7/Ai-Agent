using Microsoft.SemanticKernel;
using System.ComponentModel;
using Microsoft.SemanticKernel.ChatCompletion;
using ChatHistory = Microsoft.SemanticKernel.ChatCompletion.ChatHistory;
using SemanticKernel.AIAgentBackend.Repositories.Interface;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace SemanticKernel.AIAgentBackend.Plugins.NativePlugin
{
    public class BasicChatPlugin
    {
        private readonly Kernel _kernel;
        private readonly IChatHistoryService _chatService;
        private readonly Guid sessionId;
        private const string SystemMessage =
        "You are an AI assistant that provides precise, professional, and context-aware responses. " +
        "Maintain conversation history, summarize key points when needed, and ensure concise yet informative answers.";

        public BasicChatPlugin([FromKeyedServices("LLMKernel")] Kernel kernel, IChatHistoryService chatService, Guid sessionId)
        {
            _kernel = kernel;
            _chatService = chatService;
            this.sessionId = sessionId;
        }

        [KernelFunction("chat"), Description("Retrieves historical conversation data and serves as a fallback when other plugins lack the required context. This function is executed by the planner if the user's query relates to past interactions or ambiguous information.")]
        public async Task<string> ChatAsync([Description("User query")] string query, CancellationToken cancellationToken)
        {
            try
            {
                var openAIPromptExecutionSettings = new OpenAIPromptExecutionSettings
                {
                    Temperature = 0.7,
                    TopP = 0.9,
                    MaxTokens = 100,
                    ChatSystemPrompt = SystemMessage
                };

                var chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();

                ChatHistory chatHistory = new ChatHistory();

                var userChatHistory = await _chatService.GetMessagesAsync(sessionId, new Guid(), 10, cancellationToken);

                foreach (var chat in userChatHistory)
                {
                    if(chat.Sender == "User")
                        chatHistory.AddUserMessage(chat.Message!);
                    else
                        chatHistory.AddAssistantMessage(chat.Message!);
                }

                //current user message
                chatHistory.AddUserMessage(query);

                var result = await chatCompletionService.GetChatMessageContentAsync(chatHistory: chatHistory, kernel: _kernel, executionSettings: openAIPromptExecutionSettings);

                //var result = chatCompletionService.GetStreamingChatMessageContentsAsync(chatHistory: chatHistory, kernel: _kernel, executionSettings: openAIPromptExecutionSettings);

                //var response = new StringBuilder();
                //await foreach (var chunk in result)
                //{
                //    response.Append(chunk);
                //}

                return result.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ChatAsync: {ex.Message}");
                return "Sorry, an error occurred while processing your request.";
            }
        }
    }
}