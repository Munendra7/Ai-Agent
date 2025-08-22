using SemanticKernel.AIAgentBackend.Models.DTO;
using System.Text.Json;
using System.Text;
using System.Web;

namespace SemanticKernel.AIAgentBackend.Services
{
    public class OAuthService : IOAuthService
    {
        private readonly IConfiguration _configuration;
        private readonly IUserService _userService;
        private readonly IJwtService _jwtService;
        private readonly HttpClient _httpClient;

        public OAuthService(IConfiguration configuration, IUserService userService, IJwtService jwtService, HttpClient httpClient)
        {
            _configuration = configuration;
            _userService = userService;
            _jwtService = jwtService;
            _httpClient = httpClient;
        }

        public string GetGoogleAuthUrl()
        {
            var clientId = _configuration["OAuth:Google:ClientId"];
            var redirectUri = _configuration["OAuth:Google:RedirectUri"];
            var scope = "openid email profile";
            var state = Guid.NewGuid().ToString();

            var authUrl = $"https://accounts.google.com/o/oauth2/v2/auth?" +
                         $"client_id={clientId}&" +
                         $"redirect_uri={HttpUtility.UrlEncode(redirectUri)}&" +
                         $"response_type=code&" +
                         $"scope={HttpUtility.UrlEncode(scope)}&" +
                         $"state={state}";

            return authUrl;
        }

        public string GetMicrosoftAuthUrl()
        {
            var clientId = _configuration["OAuth:Microsoft:ClientId"];
            var redirectUri = _configuration["OAuth:Microsoft:RedirectUri"];
            var scope = "openid email profile";
            var state = Guid.NewGuid().ToString();

            var authUrl = $"https://login.microsoftonline.com/common/oauth2/v2.0/authorize?" +
                         $"client_id={clientId}&" +
                         $"redirect_uri={HttpUtility.UrlEncode(redirectUri)}&" +
                         $"response_type=code&" +
                         $"scope={HttpUtility.UrlEncode(scope)}&" +
                         $"state={state}";

            return authUrl;
        }

        public string GetGitHubAuthUrl()
        {
            var clientId = _configuration["OAuth:GitHub:ClientId"];
            var redirectUri = _configuration["OAuth:GitHub:RedirectUri"];
            var scope = "user:email";
            var state = Guid.NewGuid().ToString();

            var authUrl = $"https://github.com/login/oauth/authorize?" +
                         $"client_id={clientId}&" +
                         $"redirect_uri={HttpUtility.UrlEncode(redirectUri)}&" +
                         $"scope={HttpUtility.UrlEncode(scope)}&" +
                         $"state={state}";

            return authUrl;
        }

        public async Task<AuthResponseDto> HandleGoogleCallbackAsync(string code)
        {
            // Exchange code for access token
            var tokenResponse = await ExchangeGoogleCodeForTokenAsync(code);
            var userInfo = await GetGoogleUserInfoAsync(tokenResponse.AccessToken);

            // Get or create user
            var user = await _userService.GetUserByProviderAsync("Google", userInfo.Id);
            if (user == null)
            {
                user = await _userService.CreateUserAsync(
                    userInfo.Email,
                    userInfo.Name,
                    "Google",
                    userInfo.Id,
                    userInfo.Picture
                );
            }
            else
            {
                user = await _userService.UpdateLastLoginAsync(user);
            }

            // Generate JWT token
            var jwtToken = _jwtService.GenerateToken(user);
            var userDto = await _userService.MapToUserDto(user);

            return new AuthResponseDto
            {
                Token = jwtToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(int.Parse(_configuration["JwtSettings:ExpiryMinutes"] ?? "60")),
                User = userDto
            };
        }

        public async Task<AuthResponseDto> HandleMicrosoftCallbackAsync(string code)
        {
            // Exchange code for access token
            var tokenResponse = await ExchangeMicrosoftCodeForTokenAsync(code);
            var userInfo = await GetMicrosoftUserInfoAsync(tokenResponse.AccessToken);

            // Get or create user
            var user = await _userService.GetUserByProviderAsync("Microsoft", userInfo.Id);
            if (user == null)
            {
                user = await _userService.CreateUserAsync(
                    userInfo.Mail ?? userInfo.UserPrincipalName,
                    userInfo.DisplayName,
                    "Microsoft",
                    userInfo.Id
                );
            }
            else
            {
                user = await _userService.UpdateLastLoginAsync(user);
            }

            // Generate JWT token
            var jwtToken = _jwtService.GenerateToken(user);
            var userDto = await _userService.MapToUserDto(user);

            return new AuthResponseDto
            {
                Token = jwtToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(int.Parse(_configuration["JwtSettings:ExpiryMinutes"] ?? "60")),
                User = userDto
            };
        }

        public async Task<AuthResponseDto> HandleGitHubCallbackAsync(string code)
        {
            // Exchange code for access token
            var tokenResponse = await ExchangeGitHubCodeForTokenAsync(code);
            var userInfo = await GetGitHubUserInfoAsync(tokenResponse.AccessToken);

            // Get or create user
            var user = await _userService.GetUserByProviderAsync("GitHub", userInfo.Id.ToString());
            if (user == null)
            {
                user = await _userService.CreateUserAsync(
                    userInfo.Email ?? $"{userInfo.Login}@github.com",
                    userInfo.Name ?? userInfo.Login,
                    "GitHub",
                    userInfo.Id.ToString(),
                    userInfo.AvatarUrl
                );
            }
            else
            {
                user = await _userService.UpdateLastLoginAsync(user);
            }

            // Generate JWT token
            var jwtToken = _jwtService.GenerateToken(user);
            var userDto = await _userService.MapToUserDto(user);

            return new AuthResponseDto
            {
                Token = jwtToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(int.Parse(_configuration["JwtSettings:ExpiryMinutes"] ?? "60")),
                User = userDto
            };
        }

        private async Task<TokenResponse> ExchangeGoogleCodeForTokenAsync(string code)
        {
            var clientId = _configuration["OAuth:Google:ClientId"];
            var clientSecret = _configuration["OAuth:Google:ClientSecret"];
            var redirectUri = _configuration["OAuth:Google:RedirectUri"];

            var tokenRequest = new Dictionary<string, string>
            {
                {"code", code},
                {"client_id", clientId!},
                {"client_secret", clientSecret!},
                {"redirect_uri", redirectUri!},
                {"grant_type", "authorization_code"}
            };

            var content = new FormUrlEncodedContent(tokenRequest);
            var response = await _httpClient.PostAsync("https://oauth2.googleapis.com/token", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Failed to exchange code for token: {responseContent}");

            return JsonSerializer.Deserialize<TokenResponse>(responseContent)!;
        }

        private async Task<GoogleUserInfo> GetGoogleUserInfoAsync(string accessToken)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            var response = await _httpClient.GetAsync("https://www.googleapis.com/oauth2/v2/userinfo");
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Failed to get user info: {responseContent}");

            return JsonSerializer.Deserialize<GoogleUserInfo>(responseContent)!;
        }

        private async Task<TokenResponse> ExchangeMicrosoftCodeForTokenAsync(string code)
        {
            var clientId = _configuration["OAuth:Microsoft:ClientId"];
            var clientSecret = _configuration["OAuth:Microsoft:ClientSecret"];
            var redirectUri = _configuration["OAuth:Microsoft:RedirectUri"];

            var tokenRequest = new Dictionary<string, string>
            {
                {"code", code},
                {"client_id", clientId!},
                {"client_secret", clientSecret!},
                {"redirect_uri", redirectUri!},
                {"grant_type", "authorization_code"},
                {"scope", "openid email profile"}
            };

            var content = new FormUrlEncodedContent(tokenRequest);
            var response = await _httpClient.PostAsync("https://login.microsoftonline.com/common/oauth2/v2.0/token", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Failed to exchange code for token: {responseContent}");

            return JsonSerializer.Deserialize<TokenResponse>(responseContent)!;
        }

        private async Task<MicrosoftUserInfo> GetMicrosoftUserInfoAsync(string accessToken)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            var response = await _httpClient.GetAsync("https://graph.microsoft.com/v1.0/me");
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Failed to get user info: {responseContent}");

            return JsonSerializer.Deserialize<MicrosoftUserInfo>(responseContent)!;
        }

        private async Task<TokenResponse> ExchangeGitHubCodeForTokenAsync(string code)
        {
            var clientId = _configuration["OAuth:GitHub:ClientId"];
            var clientSecret = _configuration["OAuth:GitHub:ClientSecret"];

            var tokenRequest = new
            {
                client_id = clientId,
                client_secret = clientSecret,
                code = code
            };

            var json = JsonSerializer.Serialize(tokenRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            var response = await _httpClient.PostAsync("https://github.com/login/oauth/access_token", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Failed to exchange code for token: {responseContent}");

            return JsonSerializer.Deserialize<TokenResponse>(responseContent)!;
        }

        private async Task<GitHubUserInfo> GetGitHubUserInfoAsync(string accessToken)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            _httpClient.DefaultRequestHeaders.UserAgent.Clear();
            _httpClient.DefaultRequestHeaders.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("YourApp", "1.0"));

            var response = await _httpClient.GetAsync("https://api.github.com/user");
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Failed to get user info: {responseContent}");

            return JsonSerializer.Deserialize<GitHubUserInfo>(responseContent)!;
        }
    }

    public class TokenResponse
    {
        public string access_token { get; set; } = string.Empty;
        public string AccessToken => access_token;
        public string token_type { get; set; } = string.Empty;
        public int expires_in { get; set; }
    }

    public class GoogleUserInfo
    {
        public string id { get; set; } = string.Empty;
        public string Id => id;
        public string email { get; set; } = string.Empty;
        public string Email => email;
        public string name { get; set; } = string.Empty;
        public string Name => name;
        public string picture { get; set; } = string.Empty;
        public string Picture => picture;
    }

    public class MicrosoftUserInfo
    {
        public string id { get; set; } = string.Empty;
        public string Id => id;
        public string mail { get; set; } = string.Empty;
        public string Mail => mail;
        public string userPrincipalName { get; set; } = string.Empty;
        public string UserPrincipalName => userPrincipalName;
        public string displayName { get; set; } = string.Empty;
        public string DisplayName => displayName;
    }

    public class GitHubUserInfo
    {
        public int id { get; set; }
        public int Id => id;
        public string login { get; set; } = string.Empty;
        public string Login => login;
        public string email { get; set; } = string.Empty;
        public string Email => email;
        public string name { get; set; } = string.Empty;
        public string Name => name;
        public string avatar_url { get; set; } = string.Empty;
        public string AvatarUrl => avatar_url;
    }
}