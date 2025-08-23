using SemanticKernel.AIAgentBackend.Models.Domain;
using System.Security.Claims;

namespace SemanticKernel.AIAgentBackend.Services
{
    public interface ITokenService
    {
        string GenerateAccessToken(User user, IEnumerable<string> roles);
        RefreshToken GenerateRefreshToken(string ipAddress);
        ClaimsPrincipal? ValidateToken(string token);
    }
}
