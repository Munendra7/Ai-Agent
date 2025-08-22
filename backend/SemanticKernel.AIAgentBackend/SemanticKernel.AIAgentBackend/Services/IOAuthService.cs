using SemanticKernel.AIAgentBackend.Models.DTO;

namespace SemanticKernel.AIAgentBackend.Services
{
    public interface IOAuthService
    {
        Task<AuthResponseDto> HandleGoogleCallbackAsync(string code);
        Task<AuthResponseDto> HandleMicrosoftCallbackAsync(string code);
        Task<AuthResponseDto> HandleGitHubCallbackAsync(string code);
        string GetGoogleAuthUrl();
        string GetMicrosoftAuthUrl();
        string GetGitHubAuthUrl();
    }
}