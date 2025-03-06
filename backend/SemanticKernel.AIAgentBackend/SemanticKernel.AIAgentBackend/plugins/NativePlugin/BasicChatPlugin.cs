using Microsoft.SemanticKernel;
using SemanticKernel.AIAgentBackend.Repositories;
using System.ComponentModel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text;
using ChatHistory = Microsoft.SemanticKernel.ChatCompletion.ChatHistory;

namespace SemanticKernel.AIAgentBackend.Plugins.NativePlugin
{
    public class BasicChatPlugin
    {
        private readonly IKernelService _kernel;
        private readonly string _modelName;
        private readonly IChatService _chatService;
        private readonly string userId;

        public BasicChatPlugin(IKernelService kernel, string modelName, IChatService chatService, string userId)
        {
            _kernel = kernel;
            _modelName = modelName;
            _chatService = chatService;
            this.userId = userId;
        }

        [KernelFunction("chat"), Description("Retrieves historical conversation data and serves as a fallback when other plugins lack the required context. This function is executed by the planner if the user's query relates to past interactions or ambiguous information.")]
        public async Task<string> ChatAsync([Description("User query")] string query)
        {
            try
            {
                var kernel = _kernel.GetKernel(_modelName);
                var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

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

                var result = chatCompletionService.GetStreamingChatMessageContentsAsync(chatHistory: chatHistory, kernel: kernel);

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