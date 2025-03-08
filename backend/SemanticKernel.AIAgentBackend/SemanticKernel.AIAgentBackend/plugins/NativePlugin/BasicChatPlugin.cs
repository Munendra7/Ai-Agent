using Microsoft.SemanticKernel;
using System.ComponentModel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text;
using ChatHistory = Microsoft.SemanticKernel.ChatCompletion.ChatHistory;
using SemanticKernel.AIAgentBackend.Repositories.Interface;

namespace SemanticKernel.AIAgentBackend.Plugins.NativePlugin
{
    public class BasicChatPlugin
    {
        private readonly Kernel _kernel;
        private readonly IChatService _chatService;
        private readonly string userId;

        public BasicChatPlugin([FromKeyedServices("LLMKernel")] Kernel kernel, IChatService chatService, string userId)
        {
            _kernel = kernel;
            _chatService = chatService;
            this.userId = userId;
        }

        [KernelFunction("chat"), Description("Retrieves historical conversation data and serves as a fallback when other plugins lack the required context. This function is executed by the planner if the user's query relates to past interactions or ambiguous information.")]
        public async Task<string> ChatAsync([Description("User query")] string query)
        {
            try
            {
                var chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();

                ChatHistory chatHistory = new ChatHistory();

                var userChatHistory = await _chatService.GetMessagesAsync(userId);

                foreach (var chat in userChatHistory)
                {
                    if(chat.Sender == "User")
                        chatHistory.AddUserMessage(chat.Message!);
                    else
                        chatHistory.AddAssistantMessage(chat.Message!);
                }

                //current user message
                chatHistory.AddUserMessage(query);

                var result = chatCompletionService.GetStreamingChatMessageContentsAsync(chatHistory: chatHistory, kernel: _kernel);

                var response = new StringBuilder();
                await foreach (var chunk in result)
                {
                    response.Append(chunk);
                }

                return response.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ChatAsync: {ex.Message}");
                return "Sorry, an error occurred while processing your request.";
            }
        }
    }
}