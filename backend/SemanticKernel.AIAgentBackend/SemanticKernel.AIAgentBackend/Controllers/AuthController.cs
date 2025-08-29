using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SemanticKernel.AIAgentBackend.Models.DTO;
using SemanticKernel.AIAgentBackend.Services.Interface;

namespace SemanticKernel.AIAgentBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IOAuthService _oauthService;

        public AuthController(IAuthService authService, IOAuthService oauthService)
        {
            _authService = authService;
            _oauthService = oauthService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDTO request)
        {
            var ipAddress = GetIpAddress();
            var result = await _authService.RegisterAsync(request, ipAddress);

            if (result == null)
                return BadRequest(new { message = "Email already registered" });

            SetTokenCookie(result.RefreshToken);
            return Ok(result);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDTO request)
        {
            var ipAddress = GetIpAddress();
            var result = await _authService.LoginAsync(request, ipAddress);

            if (result == null)
                return BadRequest(new { message = "Invalid email or password" });

            SetTokenCookie(result.RefreshToken);
            return Ok(result);
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDTO? request)
        {
            var refreshToken = request?.RefreshToken ?? Request.Cookies["refreshToken"];

            if (string.IsNullOrEmpty(refreshToken))
                return BadRequest(new { message = "Refresh token is required" });

            var ipAddress = GetIpAddress();
            var result = await _authService.RefreshTokenAsync(refreshToken, ipAddress);

            if (result == null)
                return Unauthorized(new { message = "Invalid or expired refresh token" });

            SetTokenCookie(result.RefreshToken);
            return Ok(result);
        }

        [HttpPost("revoke-token")]
        [Authorize]
        public async Task<IActionResult> RevokeToken([FromBody] RefreshTokenRequestDTO? request)
        {
            var token = request?.RefreshToken ?? Request.Cookies["refreshToken"];

            if (string.IsNullOrEmpty(token))
                return BadRequest(new { message = "Token is required" });

            var ipAddress = GetIpAddress();
            await _authService.RevokeTokenAsync(token, ipAddress);

            return Ok(new { message = "Token revoked" });
        }

        [HttpPost("google")]
        public async Task<IActionResult> GoogleLogin([FromBody] ExternalAuthRequestDTO request)
        {
            var userInfo = await _oauthService.GetGoogleUserInfoAsync(request.Code, request.RedirectUri);

            if (userInfo == null)
                return BadRequest(new { message = "Invalid authorization code" });

            var ipAddress = GetIpAddress();
            var result = await _authService.ExternalLoginAsync("google", userInfo, ipAddress);

            if (result == null)
                return BadRequest(new { message = "Login failed" });

            SetTokenCookie(result.RefreshToken);
            return Ok(result);
        }

        [HttpPost("microsoft/token")]
        public async Task<IActionResult> MicrosoftTokenExchange([FromBody] MicrosoftTokenRequestDTO request)
        {
            try
            {
                var userInfo = await _oauthService.GetMicrosoftUserInfoAsync(request.IdToken);

                if (userInfo == null)
                    return BadRequest(new { message = "Invalid authorization code" });

                var ipAddress = GetIpAddress();
                var result = await _authService.ExternalLoginAsync("microsoft", userInfo, ipAddress);

                if (result == null)
                    return BadRequest(new { message = "Login failed" });

                SetTokenCookie(result.RefreshToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Token validation failed", error = ex.Message });
            }
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var refreshToken = Request.Cookies["refreshToken"];
            if (!string.IsNullOrEmpty(refreshToken))
            {
                var ipAddress = GetIpAddress();
                await _authService.RevokeTokenAsync(refreshToken, ipAddress);
            }

            Response.Cookies.Delete("refreshToken");
            return Ok(new { message = "Logged out successfully" });
        }

        private void SetTokenCookie(string token)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTime.UtcNow.AddDays(7),
                SameSite = SameSiteMode.None,
                Secure = true
            };
            Response.Cookies.Append("refreshToken", token, cookieOptions);
        }

        private string GetIpAddress()
        {
            if (Request.Headers.ContainsKey("X-Forwarded-For"))
                return Request.Headers["X-Forwarded-For"].ToString();

            return HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "unknown";
        }
    }
}
