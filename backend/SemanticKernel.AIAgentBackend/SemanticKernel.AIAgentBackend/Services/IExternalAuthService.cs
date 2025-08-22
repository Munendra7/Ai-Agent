using SemanticKernel.AIAgentBackend.Models.DTO;

namespace SemanticKernel.AIAgentBackend.Services
{
    public interface IExternalAuthService
    {
        Task<ExternalUserInfo?> ValidateGoogleTokenAsync(string accessToken);
        Task<ExternalUserInfo?> ValidateMicrosoftTokenAsync(string accessToken);
        Task<ExternalUserInfo?> ValidateGitHubTokenAsync(string accessToken);
    }
}