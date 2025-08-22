using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace SemanticKernel.AIAgentBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GitHubController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public GitHubController(IConfiguration configuration, HttpClient httpClient)
        {
            _configuration = configuration;
            _httpClient = httpClient;
        }

        [HttpPost("exchange-code")]
        public async Task<IActionResult> ExchangeCode([FromBody] GitHubCodeRequest request)
        {
            try
            {
                var clientId = _configuration["ExternalAuth:GitHub:ClientId"];
                var clientSecret = _configuration["ExternalAuth:GitHub:ClientSecret"];

                if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
                {
                    return BadRequest(new { error = "GitHub OAuth not configured" });
                }

                var tokenRequest = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("client_id", clientId),
                    new KeyValuePair<string, string>("client_secret", clientSecret),
                    new KeyValuePair<string, string>("code", request.Code),
                    new KeyValuePair<string, string>("redirect_uri", "http://localhost:5173/oauth-redirect"),
                });

                var response = await _httpClient.PostAsync("https://github.com/login/oauth/access_token", tokenRequest);
                
                if (!response.IsSuccessStatusCode)
                {
                    return BadRequest(new { error = "Failed to exchange code for token" });
                }

                var content = await response.Content.ReadAsStringAsync();
                var tokenResponse = JsonSerializer.Deserialize<GitHubTokenResponse>(content);

                if (tokenResponse?.AccessToken == null)
                {
                    return BadRequest(new { error = "Invalid token response" });
                }

                return Ok(new { access_token = tokenResponse.AccessToken });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        private class GitHubCodeRequest
        {
            public string Code { get; set; } = string.Empty;
            public string? State { get; set; }
        }

        private class GitHubTokenResponse
        {
            public string? AccessToken { get; set; }
            public string? TokenType { get; set; }
            public string? Scope { get; set; }
        }
    }
}