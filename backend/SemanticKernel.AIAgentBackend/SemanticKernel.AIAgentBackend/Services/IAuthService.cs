using Microsoft.AspNetCore.Identity.Data;
using SemanticKernel.AIAgentBackend.Models.DTO;

namespace SemanticKernel.AIAgentBackend.Services
{
    public interface IAuthService
    {
        Task<AuthResponseDTO?> RegisterAsync(RegisterRequestDTO request, string ipAddress);
        Task<AuthResponseDTO?> LoginAsync(LoginRequestDTO request, string ipAddress);
        Task<AuthResponseDTO?> RefreshTokenAsync(string token, string ipAddress);
        Task<AuthResponseDTO?> ExternalLoginAsync(string provider, OAuthUserInfo userInfo, string ipAddress);
        Task RevokeTokenAsync(string token, string ipAddress);
    }
}
