using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Planning.Handlebars;
using SemanticKernel.AIAgentBackend.CustomActionFilters;
using SemanticKernel.AIAgentBackend.Models.Domain;
using SemanticKernel.AIAgentBackend.Models.DTO;
using SemanticKernel.AIAgentBackend.plugins.NativePlugin;
using SemanticKernel.AIAgentBackend.Plugins.NativePlugin;
using SemanticKernel.AIAgentBackend.Repositories.Interface;
using SemanticKernel.AIAgentBackend.Repositories.Repository;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;
using ChatHistory = SemanticKernel.AIAgentBackend.Models.Domain.ChatHistory;

namespace SemanticKernel.AIAgentBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly Kernel _kernel;
        private readonly IConfiguration _configuration;
        private readonly IChatService _chatService;
        private readonly IEmbeddingService _embeddingService;
        private readonly ILogger _logger;

        public ChatController([FromKeyedServices("LLMKernel")] Kernel kernel, IConfiguration configuration, IChatService chatService, IEmbeddingService embeddingService, ILogger<ChatController> logger)
        {
            _kernel = kernel;
            _configuration = configuration;
            _chatService = chatService;
            _embeddingService = embeddingService;
            _logger = logger;
        }

        [HttpPost]
        [ValidateModel]
        public async Task<IActionResult> ChatAsync([FromBody] UserQueryDTO userQueryDTO)
        {
            if (string.IsNullOrWhiteSpace(userQueryDTO.Query))
            {
                return BadRequest("Question cannot be empty.");
            }

            var sessionId = new Guid("4963c532-26f9-4bea-92ae-67c0c3c05700");
            
            try
            {
                OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
                {
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
                    MaxTokens = 1000,
                    Temperature = 0.2,
                    TopP = 0.5,
                };

                var chatPlugin = new BasicChatPlugin(_kernel, _chatService, sessionId);
                var weatherPlugin = new WeatherPlugin(_kernel, _configuration);
                var googleSearchPlugin = new GoogleSearchPlugin(_kernel, _configuration);
                var ragPlugin = new RAGPlugin(_kernel, _embeddingService);

                _kernel.ImportPluginFromObject(weatherPlugin, "WeatherPlugin");
                _kernel.ImportPluginFromObject(googleSearchPlugin, "GoogleSearchPlugin");
                _kernel.ImportPluginFromObject(chatPlugin, "BasicChatPlugin");
                _kernel.ImportPluginFromObject(ragPlugin, "RAGPlugin");

                var chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();

                Microsoft.SemanticKernel.ChatCompletion.ChatHistory chatHistory = new Microsoft.SemanticKernel.ChatCompletion.ChatHistory();

                var userChatHistory = await _chatService.GetMessagesAsync(sessionId, 10);

                foreach (var chat in userChatHistory)
                {
                    if (chat.Sender == "User")
                        chatHistory.AddUserMessage(chat.Message!);
                    else
                        chatHistory.AddAssistantMessage(chat.Message!);
                }

                //current user message
                chatHistory.AddUserMessage(userQueryDTO.Query);

                var result = await chatCompletionService.GetChatMessageContentAsync(chatHistory: chatHistory, kernel: _kernel, executionSettings: openAIPromptExecutionSettings);

                var response = new ChatResponseDTO()
                {
                    Response = result?.Content?.ToString() ?? string.Empty
                };

                await _chatService.AddMessagesAsync(
                    new List<ChatHistory>()
                    {
                        new ChatHistory()
                        {
                            SessionId = sessionId,
                            Message = userQueryDTO.Query,
                            Sender = "User",
                            Timestamp = DateTime.Now
                        },
                        new ChatHistory()
                        {
                            SessionId = sessionId,
                            Message = result?.Content?.ToString() ?? string.Empty,
                            Sender = "Assistant",
                            Timestamp = DateTime.Now
                        }
                    }
                );

                return Ok(response);
            }

            catch (Exception ex)
            {
                var errorId = Guid.NewGuid();
                _logger.LogError(ex, $"{errorId} : ${ex.Message}");
                try
                {
                    var chatResponse = await _kernel.InvokeAsync("BasicChatPlugin", "chat", new() { ["query"] = userQueryDTO.Query });

                    var response = new ChatResponseDTO()
                    {
                        Response = chatResponse.ToString()
                    };

                    await _chatService.AddMessagesAsync(
                        new List<ChatHistory>()
                        {
                            new ChatHistory()
                            {
                                SessionId = sessionId,
                                Message = userQueryDTO.Query,
                                Sender = "User",
                                Timestamp = DateTime.Now
                            },
                            new ChatHistory()
                            {
                                SessionId = sessionId,
                                Message = chatResponse.ToString(),
                                Sender = "Assistant",
                                Timestamp = DateTime.Now
                            }
                        }
                    );

                    return Ok(response);
                }

                catch (Exception exe)
                {
                    _logger.LogError(ex, $"{errorId} : ${exe.Message}");

                    return StatusCode(500, new
                    {
                        error = "Something went wrong on our end. Please try again later.",
                    });
                }
            }
        }
    }
}