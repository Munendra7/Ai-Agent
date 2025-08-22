using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using SemanticKernel.AIAgentBackend.Models.DTO;

namespace SemanticKernel.AIAgentBackend.Services
{
    public class ExternalAuthService : IExternalAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public ExternalAuthService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<ExternalUserInfo?> ValidateGoogleTokenAsync(string accessToken)
        {
            try
            {
                var response = await _httpClient.GetAsync($"https://www.googleapis.com/oauth2/v2/userinfo?access_token={accessToken}");
                
                if (!response.IsSuccessStatusCode)
                    return null;

                var userInfo = await response.Content.ReadFromJsonAsync<GoogleUserInfo>();
                
                if (userInfo == null)
                    return null;

                return new ExternalUserInfo
                {
                    Id = userInfo.Id,
                    Email = userInfo.Email,
                    FirstName = userInfo.GivenName,
                    LastName = userInfo.FamilyName,
                    ProfilePictureUrl = userInfo.Picture,
                    Provider = "Google"
                };
            }
            catch
            {
                return null;
            }
        }

        public async Task<ExternalUserInfo?> ValidateMicrosoftTokenAsync(string accessToken)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                var response = await _httpClient.GetAsync("https://graph.microsoft.com/v1.0/me");
                
                if (!response.IsSuccessStatusCode)
                    return null;

                var userInfo = await response.Content.ReadFromJsonAsync<MicrosoftUserInfo>();
                
                if (userInfo == null)
                    return null;

                return new ExternalUserInfo
                {
                    Id = userInfo.Id,
                    Email = userInfo.UserPrincipalName,
                    FirstName = userInfo.GivenName,
                    LastName = userInfo.Surname,
                    ProfilePictureUrl = null, // Microsoft Graph doesn't provide profile picture in basic profile
                    Provider = "Microsoft"
                };
            }
            catch
            {
                return null;
            }
        }

        public async Task<ExternalUserInfo?> ValidateGitHubTokenAsync(string accessToken)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "YourApp");
                
                var response = await _httpClient.GetAsync("https://api.github.com/user");
                
                if (!response.IsSuccessStatusCode)
                    return null;

                var userInfo = await response.Content.ReadFromJsonAsync<GitHubUserInfo>();
                
                if (userInfo == null)
                    return null;

                // Get user email
                var emailResponse = await _httpClient.GetAsync("https://api.github.com/user/emails");
                string? email = null;
                
                if (emailResponse.IsSuccessStatusCode)
                {
                    var emails = await emailResponse.Content.ReadFromJsonAsync<List<GitHubEmail>>();
                    email = emails?.FirstOrDefault(e => e.Primary)?.Email ?? emails?.FirstOrDefault()?.Email;
                }

                return new ExternalUserInfo
                {
                    Id = userInfo.Id.ToString(),
                    Email = email ?? userInfo.Email ?? "",
                    FirstName = userInfo.Name?.Split(' ').FirstOrDefault(),
                    LastName = userInfo.Name?.Split(' ').Skip(1).FirstOrDefault(),
                    ProfilePictureUrl = userInfo.AvatarUrl,
                    Provider = "GitHub"
                };
            }
            catch
            {
                return null;
            }
        }

        private class GoogleUserInfo
        {
            public string Id { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string GivenName { get; set; } = string.Empty;
            public string FamilyName { get; set; } = string.Empty;
            public string Picture { get; set; } = string.Empty;
        }

        private class MicrosoftUserInfo
        {
            public string Id { get; set; } = string.Empty;
            public string UserPrincipalName { get; set; } = string.Empty;
            public string GivenName { get; set; } = string.Empty;
            public string Surname { get; set; } = string.Empty;
        }

        private class GitHubUserInfo
        {
            public long Id { get; set; }
            public string Login { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string AvatarUrl { get; set; } = string.Empty;
        }

        private class GitHubEmail
        {
            public string Email { get; set; } = string.Empty;
            public bool Primary { get; set; }
        }
    }
}