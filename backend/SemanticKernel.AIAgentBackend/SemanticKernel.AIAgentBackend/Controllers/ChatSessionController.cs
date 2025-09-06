using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
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
        public async Task<IActionResult> GetChatHistory(Guid sessionId)
        {
            var userId = _authService.GetUserId();
            if (userId == null)
            {
                return Unauthorized();
            }
            var history = await _chatService.GetMessagesAsync(sessionId, new Guid(userId), 200);
            return Ok(history);
        }

        [HttpGet]
        public async Task<IActionResult> GetChatSessions()
        {
            var userId = _authService.GetUserId();
            if (userId == null)
            {
                return Unauthorized();
            }
            var sessions = await _chatService.GetSessionSummariesAsync(new Guid(userId), 100);
            return Ok(sessions);
        }
    }
}
