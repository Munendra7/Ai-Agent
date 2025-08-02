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
    public class AgentController : ControllerBase
    {
        private readonly Kernel _kernel;
        private readonly IConfiguration _configuration;
        private readonly IChatHistoryService _chatService;
        private readonly ILogger _logger;
        private readonly IAgentFactory _agentFactory;

        private const string ChatSystemPrompt = @"
            You are an AI assistant that answers queries strictly using retrieved knowledge.
            - Use the RAGPlugin to fetch relevant information before responding.
            - Always look for latest files and templates if user asks about your documents or knowledge.
            - If the user asks a question about a specific video file, use the RAGPlugin to search that video’s transcribed content by filtering with the file name.
            - Use ExcelDataAnalyzerPlugin for Excel-related queries and always ask excel file name and Sheet name before doing analysis.
            - If data is insufficient, say 'No relevant information found'—do not speculate.
            - Execute queries and actions via plugins when required.
            - Keep responses factual, concise, and context-aware.
        ";

        public AgentController([FromKeyedServices("LLMKernel")] Kernel kernel, IConfiguration configuration, IChatHistoryService chatService, IAgentFactory agentFactory, ILogger<ChatController> logger)
        {
            _kernel = kernel;
            _configuration = configuration;
            _chatService = chatService;
            _agentFactory = agentFactory;
            _logger = logger;
        }

        private async Task<(ChatCompletionAgent Agent, AgentThread AgentThread, KernelArguments Arguments)> BuildAgentThreadAsync(UserQueryDTO dto)
        {
            var history = new Microsoft.SemanticKernel.ChatCompletion.ChatHistory();
            var userHistory = await _chatService.GetMessagesAsync(dto.SessionId, 15);
            var grounding = await _chatService.GetOrUpdateGroundingSummaryAsync(dto.SessionId, userHistory.ToList());

            if (!string.IsNullOrWhiteSpace(grounding))
                history.AddSystemMessage($"Previous Summary: {grounding}");

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
                var (agent, thread, args) = await BuildAgentThreadAsync(dto);

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
                var (agent, thread, args) = await BuildAgentThreadAsync(dto);

                Response.ContentType = "text/event-stream";
                Response.Headers["Cache-Control"] = "no-cache";
                Response.Headers["X-Accel-Buffering"] = "no";
                await Response.Body.FlushAsync();

                string fullResponse = "";

                await foreach (var chunk in agent.InvokeStreamingAsync(thread, new() { KernelArguments = args }))
                {
                    var content = chunk.Message?.ToString();
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        fullResponse += content;
                        await Response.WriteAsync(content);
                        await Response.Body.FlushAsync();
                    }
                }

                await _chatService.AddMessagesAsync(new List<ChatHistory>
                {
                    new() { SessionId = dto.SessionId, Message = dto.Query, Sender = "User", Timestamp = DateTime.Now },
                    new() { SessionId = dto.SessionId, Message = fullResponse, Sender = "Assistant", Timestamp = DateTime.Now }
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