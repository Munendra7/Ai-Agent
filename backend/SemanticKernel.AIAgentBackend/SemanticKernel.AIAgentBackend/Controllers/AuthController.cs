using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SemanticKernel.AIAgentBackend.Models.DTO;
using SemanticKernel.AIAgentBackend.Services;
using System.Security.Claims;

namespace SemanticKernel.AIAgentBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IOAuthService _oauthService;
        private readonly IUserService _userService;
        private readonly IJwtService _jwtService;

        public AuthController(IOAuthService oauthService, IUserService userService, IJwtService jwtService)
        {
            _oauthService = oauthService;
            _userService = userService;
            _jwtService = jwtService;
        }

        [HttpGet("google/login")]
        public IActionResult GoogleLogin()
        {
            var authUrl = _oauthService.GetGoogleAuthUrl();
            return Ok(new { authUrl });
        }

        [HttpGet("microsoft/login")]
        public IActionResult MicrosoftLogin()
        {
            var authUrl = _oauthService.GetMicrosoftAuthUrl();
            return Ok(new { authUrl });
        }

        [HttpGet("github/login")]
        public IActionResult GitHubLogin()
        {
            var authUrl = _oauthService.GetGitHubAuthUrl();
            return Ok(new { authUrl });
        }

        [HttpPost("google/callback")]
        public async Task<IActionResult> GoogleCallback([FromBody] OAuthCallbackDto callback)
        {
            try
            {
                if (!string.IsNullOrEmpty(callback.Error))
                {
                    return BadRequest(new { error = callback.Error, description = callback.ErrorDescription });
                }

                var authResponse = await _oauthService.HandleGoogleCallbackAsync(callback.Code);
                return Ok(authResponse);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = "authentication_failed", message = ex.Message });
            }
        }

        [HttpPost("microsoft/callback")]
        public async Task<IActionResult> MicrosoftCallback([FromBody] OAuthCallbackDto callback)
        {
            try
            {
                if (!string.IsNullOrEmpty(callback.Error))
                {
                    return BadRequest(new { error = callback.Error, description = callback.ErrorDescription });
                }

                var authResponse = await _oauthService.HandleMicrosoftCallbackAsync(callback.Code);
                return Ok(authResponse);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = "authentication_failed", message = ex.Message });
            }
        }

        [HttpPost("github/callback")]
        public async Task<IActionResult> GitHubCallback([FromBody] OAuthCallbackDto callback)
        {
            try
            {
                if (!string.IsNullOrEmpty(callback.Error))
                {
                    return BadRequest(new { error = callback.Error, description = callback.ErrorDescription });
                }

                var authResponse = await _oauthService.HandleGitHubCallbackAsync(callback.Code);
                return Ok(authResponse);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = "authentication_failed", message = ex.Message });
            }
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized();
                }

                var user = await _userService.GetUserByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                var userDto = await _userService.MapToUserDto(user);
                return Ok(userDto);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = "failed_to_get_user", message = ex.Message });
            }
        }

        [HttpPost("refresh")]
        [Authorize]
        public async Task<IActionResult> RefreshToken()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized();
                }

                var user = await _userService.GetUserByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                var newToken = _jwtService.GenerateToken(user);
                var userDto = await _userService.MapToUserDto(user);

                return Ok(new AuthResponseDto
                {
                    Token = newToken,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(60), // Default 60 minutes
                    User = userDto
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = "refresh_failed", message = ex.Message });
            }
        }

        [HttpPost("logout")]
        [Authorize]
        public IActionResult Logout()
        {
            // Since we're using JWT tokens, logout is handled client-side
            // by removing the token from storage
            return Ok(new { message = "Logged out successfully" });
        }
    }
}