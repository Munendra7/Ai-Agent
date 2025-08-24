using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Tokens;
using SemanticKernel.AIAgentBackend.Models.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using Azure.Core;

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

        public async Task<OAuthUserInfo?> GetMicrosoftUserInfoAsync(string idToken)
        {
            // Validate the Microsoft ID token
            var validatedToken = await ValidateMicrosoftToken(idToken);

            if (validatedToken == null)
                return null;

            // Extract user info from the validated token
            var claims = validatedToken.Claims.ToDictionary(c => c.Type, c => c.Value);

            return new OAuthUserInfo
            {
                Id = claims.ContainsKey("http://schemas.microsoft.com/identity/claims/objectidentifier") ? claims["http://schemas.microsoft.com/identity/claims/objectidentifier"] : "",
                Email = claims.ContainsKey("preferred_username") ? claims["preferred_username"] : "",
                FirstName = claims.ContainsKey("given_name") ? claims["given_name"] : "",
                LastName = claims.ContainsKey("family_name") ? claims["family_name"] : ""
            };
        }

        private async Task<ClaimsPrincipal?> ValidateMicrosoftToken(string idToken)
        {
            try
            {
                var configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                    "https://login.microsoftonline.com/common/v2.0/.well-known/openid-configuration",
                    new OpenIdConnectConfigurationRetriever(),
                    new HttpDocumentRetriever());

                var openIdConfig = await configurationManager.GetConfigurationAsync();

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = true,
                    ValidAudience = _oauthSettings.Microsoft.ClientId,
                    ValidateLifetime = true,
                    IssuerSigningKeys = openIdConfig.SigningKeys,
                    ValidateIssuerSigningKey = true
                };

                var handler = new JwtSecurityTokenHandler();
                var principal = handler.ValidateToken(idToken, validationParameters, out _);
                return principal;
            }
            catch
            {
                return null;
            }
        }
    }
}
