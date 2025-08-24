using SemanticKernel.AIAgentBackend.Services.Model;

namespace SemanticKernel.AIAgentBackend.Services.Interface
{
    public interface IOAuthService
    {
        Task<OAuthUserInfo?> GetGoogleUserInfoAsync(string code, string redirectUri);
        Task<OAuthUserInfo?> GetMicrosoftUserInfoAsync(string idToken);
    }
}
