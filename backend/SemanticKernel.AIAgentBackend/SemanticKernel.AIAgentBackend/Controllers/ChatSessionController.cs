using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SemanticKernel.AIAgentBackend.Models.Domain;
using SemanticKernel.AIAgentBackend.Models.DTO;
using SemanticKernel.AIAgentBackend.Repositories.Interface;
using SemanticKernel.AIAgentBackend.Services.Interface;

namespace SemanticKernel.AIAgentBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    [DisableRateLimiting]
    public class ChatSessionController : ControllerBase
    {
        private readonly IChatHistoryService _chatService;
        private readonly IAuthService _authService;

        public ChatSessionController(IChatHistoryService chatService, IAuthService authService)
        {
            _chatService = chatService;
            _authService = authService;
        }

        [HttpGet("{sessionId}")]
        public async Task<ActionResult<PaginationResponseDTO<ChatHistory>>> GetChatHistory([FromRoute]Guid sessionId, [FromQuery]PaginationRequestDTO paginationRequestDTO, CancellationToken cancellationToken)
        {
            var userId = _authService.GetUserId();
            if (userId == null)
            {
                return Unauthorized();
            }
            var history = await _chatService.GetPagedMessagesAsync(sessionId, new Guid(userId), paginationRequestDTO, cancellationToken);
            return Ok(history);
        }

        [HttpGet]
        public async Task<ActionResult<PaginationResponseDTO<SessionSummary>>> GetChatSessions([FromQuery] PaginationRequestDTO paginationRequestDTO, CancellationToken cancellationToken)
        {
            var userId = _authService.GetUserId();
            if (userId == null)
            {
                return Unauthorized();
            }
            var sessions = await _chatService.GetPagedSessionsAsync(new Guid(userId), paginationRequestDTO, cancellationToken);
            return Ok(sessions);
        }
    }
}
