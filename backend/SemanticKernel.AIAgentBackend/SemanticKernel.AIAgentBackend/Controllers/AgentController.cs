using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using SemanticKernel.AIAgentBackend.Models.DTO;
using SemanticKernel.AIAgentBackend.Plugins.NativePlugin;
using SemanticKernel.AIAgentBackend.CustomActionFilters;
using SemanticKernel.AIAgentBackend.Factories.Interface;
using SemanticKernel.AIAgentBackend.Repositories.Interface;
using ChatHistory = SemanticKernel.AIAgentBackend.Models.Domain.ChatHistory;
using Microsoft.SemanticKernel.Agents;

namespace SemanticKernel.AIAgentBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class AgentController : ControllerBase
    {
        private readonly Kernel _kernel;
        private readonly IConfiguration _configuration;
        private readonly IChatHistoryService _chatService;
        private readonly ILogger _logger;
        private readonly IAgentFactory _agentFactory;

        public AgentController([FromKeyedServices("LLMKernel")] Kernel kernel, IConfiguration configuration, IChatHistoryService chatService, IAgentFactory agentFactory, ILogger<ChatController> logger)
        {
            _kernel = kernel;
            _configuration = configuration;
            _chatService = chatService;
            _agentFactory = agentFactory;
            _logger = logger;
        }

        [Route("SingleAgentChat")]
        [HttpPost]
        [ValidateModel]
        public async Task<IActionResult> SingleAgentChat([FromBody] UserQueryDTO userQueryDTO)
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
                - Always look for latest files and templates if user asks about your documents or knowledge.
                - If the user asks a question about a specific video file, use the RAGPlugin to search that video’s transcribed content by filtering with the file name.
                - Use ExcelDataAnalyzerPlugin for Excel-related queries and always ask excel file name and Sheet name before doing analysis.
                - If data is insufficient, say 'No relevant information found'—do not speculate.  
                - Execute queries and actions via plugins when required.  
                - Keep responses factual, concise, and context-aware.  
                ";

                var chatHistory = new Microsoft.SemanticKernel.ChatCompletion.ChatHistory();

                var userChatHistory = await _chatService.GetMessagesAsync(userQueryDTO.SessionId, 15);

                string groundingSummary = await _chatService.GetOrUpdateGroundingSummaryAsync(userQueryDTO.SessionId, userChatHistory.ToList());

                if (!string.IsNullOrWhiteSpace(groundingSummary))
                {
                    chatHistory.AddSystemMessage($"Previous Summary: {groundingSummary}");
                }

                foreach (var chat in userChatHistory)
                {
                    if (chat.Sender == "User")
                        chatHistory.AddUserMessage(chat.Message!);
                    else
                        chatHistory.AddAssistantMessage(chat.Message!);
                }

                //current user message
                chatHistory.AddUserMessage(userQueryDTO.Query);

                AgentThread agentThread = new ChatHistoryAgentThread(chatHistory);

                var agent = _agentFactory.CreateAgent(_kernel, "Agent",
                ChatSystemPrompt,
                new List<object> { });

                DateTime now = DateTime.Now;
                KernelArguments arguments =
                new()
                {
                { "now", $"{now.ToShortDateString()} {now.ToShortTimeString()}" }
                };

                string assistantMessage = "";

                await foreach (var item in agent.InvokeStreamingAsync(agentThread, new() { KernelArguments = arguments }))
                {
                    assistantMessage += item.Message?.ToString();
                }


                await _chatService.AddMessagesAsync(new List<ChatHistory>
                {
                    new ChatHistory
                    {
                        SessionId = userQueryDTO.SessionId,
                        Message = userQueryDTO.Query,
                        Sender = "User",
                        Timestamp = DateTime.Now
                    },
                    new ChatHistory
                    {
                        SessionId = userQueryDTO.SessionId,
                        Message = assistantMessage?.ToString(),
                        Sender = "Assistant",
                        Timestamp = DateTime.Now
                    }
                });

                var response = new ChatResponseDTO
                {
                    Response = assistantMessage?.ToString()
                };

                return Ok(response);
            }

            catch (Exception ex)
            {
                var errorId = Guid.NewGuid();
                _logger.LogError(ex, $"{errorId} : ${ex.Message}");
                try
                {
                    var chatPlugin = new BasicChatPlugin(_kernel, _chatService, userQueryDTO.SessionId);
                    _kernel.ImportPluginFromObject(chatPlugin, "BasicChatPlugin");

                    var chatResponse = await _kernel.InvokeAsync("BasicChatPlugin", "chat", new() { ["query"] = userQueryDTO.Query + "and Error from first attempt" + $"{errorId} : ${ex.Message}" });

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
