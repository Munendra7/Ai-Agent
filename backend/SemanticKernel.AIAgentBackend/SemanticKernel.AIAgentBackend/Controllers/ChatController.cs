using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using SemanticKernel.AIAgentBackend.CustomActionFilters;
using SemanticKernel.AIAgentBackend.Factories.Interface;
using SemanticKernel.AIAgentBackend.Models.DTO;
using SemanticKernel.AIAgentBackend.plugins.NativePlugin;
using SemanticKernel.AIAgentBackend.Plugins.NativePlugin;
using SemanticKernel.AIAgentBackend.Repositories.Interface;
using ChatHistory = SemanticKernel.AIAgentBackend.Models.Domain.ChatHistory;

namespace SemanticKernel.AIAgentBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class ChatController : ControllerBase
    {
        private readonly Kernel _kernel;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IChatHistoryService _chatService;
        private readonly IEmbeddingService _embeddingService;
        private readonly IBlobService _blobService;
        private readonly ILogger _logger;
        private readonly IDocumentsProcessFactory _documentsProcessFactory;

        public ChatController([FromKeyedServices("LLMKernel")] Kernel kernel, HttpClient httpClient, IConfiguration configuration, IChatHistoryService chatService, IEmbeddingService embeddingService, IBlobService blobService, IDocumentsProcessFactory documentsProcessFactory, ILogger<ChatController> logger)
        {
            _kernel = kernel;
            _httpClient = httpClient;
            _configuration = configuration;
            _chatService = chatService;
            _embeddingService = embeddingService;
            _blobService = blobService;
            _documentsProcessFactory = documentsProcessFactory;
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
            
            try
            {
                string ChatSystemPrompt = @"
                You are an AI assistant that answers queries strictly using retrieved knowledge. 

                - Use the RAGPlugin to fetch relevant information before responding.  
                - If data is insufficient, say 'No relevant information found'—do not speculate.  
                - Execute queries and actions via plugins when required.  
                - Keep responses factual, concise, and context-aware.  
                ";


                OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
                {
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
                    MaxTokens = 1000,
                    Temperature = 0.2,
                    TopP = 0.5,
                    ChatSystemPrompt = ChatSystemPrompt
                };

                var chatPlugin = new BasicChatPlugin(_kernel, _chatService, userQueryDTO.SessionId);
                var weatherPlugin = new WeatherPlugin(_kernel, _configuration);
                var googleSearchPlugin = new GoogleSearchPlugin(_kernel, _configuration);
                var ragPlugin = new RAGPlugin(_kernel, _embeddingService, _blobService);
                var emailwriterPlugin = new EmailWriterPlugin(_httpClient, _configuration);
                var documentGenerationPlugin = new DocumentGenerationPlugin(_blobService, _documentsProcessFactory);
                var excelDataAnalyzerPlugin = new ExcelDataAnalyzerPlugin(_kernel, _blobService);

                _kernel.ImportPluginFromObject(ragPlugin, "RAGPlugin");
                _kernel.ImportPluginFromObject(weatherPlugin, "WeatherPlugin");
                _kernel.ImportPluginFromObject(googleSearchPlugin, "GoogleSearchPlugin");
                _kernel.ImportPluginFromObject(chatPlugin, "BasicChatPlugin");
                _kernel.ImportPluginFromObject(emailwriterPlugin, "EmailWriterPlugin");
                _kernel.ImportPluginFromObject(documentGenerationPlugin, "DocumentGenerationPlugin");
                _kernel.ImportPluginFromObject(excelDataAnalyzerPlugin, "ExcelDataAnalyzerPlugin");

                var chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();

                Microsoft.SemanticKernel.ChatCompletion.ChatHistory chatHistory = new Microsoft.SemanticKernel.ChatCompletion.ChatHistory();

                var userChatHistory = await _chatService.GetMessagesAsync(userQueryDTO.SessionId, 10);

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
                            SessionId = userQueryDTO.SessionId,
                            Message = userQueryDTO.Query,
                            Sender = "User",
                            Timestamp = DateTime.Now
                        },
                        new ChatHistory()
                        {
                            SessionId = userQueryDTO.SessionId,
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
                                SessionId = userQueryDTO.SessionId,
                                Message = userQueryDTO.Query,
                                Sender = "User",
                                Timestamp = DateTime.Now
                            },
                            new ChatHistory()
                            {
                                SessionId = userQueryDTO.SessionId,
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