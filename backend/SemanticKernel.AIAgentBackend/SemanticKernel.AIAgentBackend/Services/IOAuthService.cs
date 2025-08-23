namespace SemanticKernel.AIAgentBackend.Services
{
    public interface IOAuthService
    {
        Task<OAuthUserInfo?> GetGoogleUserInfoAsync(string code, string redirectUri);
        Task<OAuthUserInfo?> GetMicrosoftUserInfoAsync(string code, string redirectUri);
    }
}
