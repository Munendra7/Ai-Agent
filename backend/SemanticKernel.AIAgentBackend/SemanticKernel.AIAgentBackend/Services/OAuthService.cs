using Microsoft.Extensions.Options;
using SemanticKernel.AIAgentBackend.Models.Configuration;
using System.Net.Http.Headers;
using System.Text.Json;

namespace SemanticKernel.AIAgentBackend.Services
{
    public class OAuthService : IOAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly OAuthSettings _oauthSettings;

        public OAuthService(HttpClient httpClient, IOptions<OAuthSettings> oauthSettings)
        {
            _httpClient = httpClient;
            _oauthSettings = oauthSettings.Value;
        }

        public async Task<OAuthUserInfo?> GetGoogleUserInfoAsync(string code, string redirectUri)
        {
            // Exchange code for token
            var tokenRequest = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("code", code),
                new KeyValuePair<string, string>("client_id", _oauthSettings.Google.ClientId),
                new KeyValuePair<string, string>("client_secret", _oauthSettings.Google.ClientSecret),
                new KeyValuePair<string, string>("redirect_uri", redirectUri),
                new KeyValuePair<string, string>("grant_type", "authorization_code")
            });

            var tokenResponse = await _httpClient.PostAsync(
                "https://oauth2.googleapis.com/token",
                tokenRequest);

            if (!tokenResponse.IsSuccessStatusCode)
                return null;

            var tokenContent = await tokenResponse.Content.ReadAsStringAsync();
            var tokenData = JsonDocument.Parse(tokenContent);
            var accessToken = tokenData.RootElement.GetProperty("access_token").GetString();

            // Get user info
            var userInfoRequest = new HttpRequestMessage(HttpMethod.Get,
                "https://www.googleapis.com/oauth2/v2/userinfo");
            userInfoRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var userInfoResponse = await _httpClient.SendAsync(userInfoRequest);
            if (!userInfoResponse.IsSuccessStatusCode)
                return null;

            var userInfoContent = await userInfoResponse.Content.ReadAsStringAsync();
            var userInfo = JsonDocument.Parse(userInfoContent);

            return new OAuthUserInfo
            {
                Id = userInfo.RootElement.TryGetProperty("id", out var idProp) ? idProp.GetString() ?? "" : "",
                Email = userInfo.RootElement.TryGetProperty("email", out var emailProp) ? emailProp.GetString() ?? "": "",
                FirstName = userInfo.RootElement.TryGetProperty("given_name", out var firstNameProp)? firstNameProp.GetString() ?? "": "",
                LastName = userInfo.RootElement.TryGetProperty("family_name", out var lastNameProp)? lastNameProp.GetString() ?? "": ""
            };
        }

        public async Task<OAuthUserInfo?> GetMicrosoftUserInfoAsync(string code, string redirectUri)
        {
            // Exchange code for token
            var tokenRequest = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("code", code),
                new KeyValuePair<string, string>("client_id", _oauthSettings.Microsoft.ClientId),
                new KeyValuePair<string, string>("client_secret", _oauthSettings.Microsoft.ClientSecret),
                new KeyValuePair<string, string>("redirect_uri", redirectUri),
                new KeyValuePair<string, string>("grant_type", "authorization_code")
            });

            var tokenUrl = $"https://login.microsoftonline.com/{_oauthSettings.Microsoft.TenantId}/oauth2/v2.0/token";
            var tokenResponse = await _httpClient.PostAsync(tokenUrl, tokenRequest);

            if (!tokenResponse.IsSuccessStatusCode)
                return null;

            var tokenContent = await tokenResponse.Content.ReadAsStringAsync();
            var tokenData = JsonDocument.Parse(tokenContent);
            var accessToken = tokenData.RootElement.GetProperty("access_token").GetString();

            // Get user info
            var userInfoRequest = new HttpRequestMessage(HttpMethod.Get,
                "https://graph.microsoft.com/v1.0/me");
            userInfoRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var userInfoResponse = await _httpClient.SendAsync(userInfoRequest);
            if (!userInfoResponse.IsSuccessStatusCode)
                return null;

            var userInfoContent = await userInfoResponse.Content.ReadAsStringAsync();
            var userInfo = JsonDocument.Parse(userInfoContent);

            return new OAuthUserInfo
            {
                Id = userInfo.RootElement.TryGetProperty("id", out var idProp) ? idProp.GetString() ?? "" : "",
                Email = userInfo.RootElement.TryGetProperty("mail", out var mailProp) && !string.IsNullOrEmpty(mailProp.GetString()) ? mailProp.GetString()! : userInfo.RootElement.TryGetProperty("userPrincipalName", out var upnProp) ? upnProp.GetString() ?? "" : "",
                FirstName = userInfo.RootElement.TryGetProperty("givenName", out var firstProp) ? firstProp.GetString() ?? "" : "",
                LastName = userInfo.RootElement.TryGetProperty("surname", out var lastProp) ? lastProp.GetString() ?? "": ""
            };
        }
    }
}
