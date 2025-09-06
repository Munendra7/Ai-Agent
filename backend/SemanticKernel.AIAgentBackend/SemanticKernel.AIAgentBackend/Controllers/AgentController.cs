using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using SemanticKernel.AIAgentBackend.Models.DTO;
using SemanticKernel.AIAgentBackend.Plugins.NativePlugin;
using SemanticKernel.AIAgentBackend.CustomActionFilters;
using SemanticKernel.AIAgentBackend.Factories.Interface;
using SemanticKernel.AIAgentBackend.Repositories.Interface;
using ChatHistory = SemanticKernel.AIAgentBackend.Models.Domain.ChatHistory;
using Microsoft.SemanticKernel.Agents;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using SemanticKernel.AIAgentBackend.Services.Interface;

namespace SemanticKernel.AIAgentBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AgentController : ControllerBase
    {
        private readonly Kernel _kernel;
        private readonly IConfiguration _configuration;
        private readonly IChatHistoryService _chatService;
        private readonly ILogger _logger;
        private readonly IAgentFactory _agentFactory;
        private readonly IAuthService _authService;

        private const string ChatSystemPrompt = @"
        You are an AI assistant that answers strictly using retrieved knowledge and available plugins. 
        Never speculate or invent information.

        # Core Rules
        - Always attempt to answer queries using **RAGPlugin** answerfromKnowledge function(retrieved knowledge).
        - For Follow up questions, always get new information using **RAGPlugin**, before answering.
        - Only filter by a specific document name if the user explicitly mentions it.
        - Do not rely on general knowledge unless the user explicitly requests it.
        - **ExcelDataAnalyzerPlugin**: For Excel queries, always request the file name and sheet name before analysis.
        - If sufficient information is not found, respond with: 'No relevant information found.'
        - Execute actions and queries exclusively through plugins when required.

        # Response Guidelines
        - Be factual, concise, and context-grounded.
        - Reference sources when applicable.
        - Never provide unsupported or speculative content.";

        public AgentController([FromKeyedServices("LLMKernel")] Kernel kernel, IConfiguration configuration, IChatHistoryService chatService, IAgentFactory agentFactory, IAuthService authService, ILogger<ChatController> logger)
        {
            _kernel = kernel;
            _configuration = configuration;
            _chatService = chatService;
            _agentFactory = agentFactory;
            _logger = logger;
            _authService = authService;
        }

        private async Task<(ChatCompletionAgent Agent, AgentThread AgentThread, KernelArguments Arguments)> BuildAgentThreadAsync(UserQueryDTO dto, Guid userId)
        {
            var history = new Microsoft.SemanticKernel.ChatCompletion.ChatHistory();
            var userHistory = await _chatService.GetMessagesAsync(dto.SessionId, userId, 15);
            
            var grounding = await _chatService.GetOrUpdateGroundingSummaryAsync(dto.SessionId, userId, userHistory.ToList(), dto.Query);

            if (!string.IsNullOrWhiteSpace(grounding))
                history.AddSystemMessage($"Grounding Context: {grounding}");

            foreach (var chat in userHistory)
                if (chat.Sender == "User")
                    history.AddUserMessage(chat.Message!);
                else
                    history.AddAssistantMessage(chat.Message!);

            history.AddUserMessage(dto.Query);

            var thread = new ChatHistoryAgentThread(history);
            var agent = _agentFactory.CreateAgent(_kernel, "Agent", ChatSystemPrompt, new List<object>());
            var arguments = new KernelArguments
            {
                ["now"] = $"{DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}"
            };

            return (agent, thread, arguments);
        }

        [Route("SingleAgentChat")]
        [HttpPost]
        [ValidateModel]
        public async Task<IActionResult> SingleAgentChat([FromBody] UserQueryDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Query))
                return BadRequest("Query cannot be empty.");

            try
            {
                var userId =  _authService.GetUserId();
                if (userId == null)
                {
                    return Unauthorized();
                }

                var (agent, thread, args) = await BuildAgentThreadAsync(dto, new Guid(userId));

                string assistantMessage = "";

                await foreach (var chunk in agent.InvokeStreamingAsync(thread, new() { KernelArguments = args }))
                    assistantMessage += chunk.Message?.ToString();

                await _chatService.AddMessagesAsync(new List<ChatHistory>
                {
                    new() { SessionId = dto.SessionId, Message = dto.Query, Sender = "User", Timestamp = DateTime.Now },
                    new() { SessionId = dto.SessionId, Message = assistantMessage, Sender = "Assistant", Timestamp = DateTime.Now }
                });

                return Ok(new ChatResponseDTO { Response = assistantMessage });
            }
            catch (Exception ex)
            {
                var errorId = Guid.NewGuid();
                _logger.LogError(ex, $"{errorId} : {ex.Message}");

                try
                {
                    var plugin = new BasicChatPlugin(_kernel, _chatService, dto.SessionId);
                    _kernel.ImportPluginFromObject(plugin, "BasicChatPlugin");

                    var fallback = await _kernel.InvokeAsync("BasicChatPlugin", "chat", new()
                    {
                        ["query"] = $"{dto.Query} (Fallback from error {errorId}: {ex.Message})"
                    });

                    var response = fallback.ToString();
                    await _chatService.AddMessagesAsync(new List<ChatHistory>
                    {
                        new() { SessionId = dto.SessionId, Message = dto.Query, Sender = "User", Timestamp = DateTime.Now },
                        new() { SessionId = dto.SessionId, Message = response, Sender = "Assistant", Timestamp = DateTime.Now }
                    });

                    return Ok(new ChatResponseDTO { Response = response });
                }
                catch (Exception fallbackEx)
                {
                    _logger.LogError(fallbackEx, $"{errorId} : {fallbackEx.Message}");
                    return StatusCode(500, new { error = "Something went wrong on our end. Please try again later." });
                }
            }
        }

        [Route("StreamAgentChat")]
        [HttpPost]
        [ValidateModel]
        public async Task StreamAgentChat([FromBody] UserQueryDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Query))
            {
                Response.StatusCode = 400;
                await Response.WriteAsync("Query cannot be empty.");
                return;
            }

            try
            {
                var userId = _authService.GetUserId();
                if (userId == null)
                {
                    Response.StatusCode = 401;
                    await Response.Body.FlushAsync();
                    return;
                }

                var (agent, thread, args) = await BuildAgentThreadAsync(dto, new Guid(userId));

                Response.ContentType = "text/event-stream";
                Response.Headers["Cache-Control"] = "no-cache";
                Response.Headers["X-Accel-Buffering"] = "no";
                await Response.Body.FlushAsync();

                string fullResponse = "";

                await foreach (var chunk in agent.InvokeStreamingAsync(thread, new() { KernelArguments = args }))
                {
                    var content = chunk.Message?.ToString();
                    if (!string.IsNullOrEmpty(content))
                    {
                        fullResponse += content;
                        await Response.WriteAsync(content);
                        await Response.Body.FlushAsync();
                    }
                }

                await _chatService.AddMessagesAsync(new List<ChatHistory>
                {
                    new() { SessionId = dto.SessionId, UserId = new Guid(userId), Message = dto.Query, Sender = "User", Timestamp = DateTime.Now },
                    new() { SessionId = dto.SessionId, UserId = new Guid(userId), Message = fullResponse, Sender = "Assistant", Timestamp = DateTime.Now }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"StreamAgentChat Error: {ex.Message}");
                Response.StatusCode = 500;
                await Response.WriteAsync("An error occurred during streaming.");
            }
        }
    }
}