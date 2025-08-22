using SemanticKernel.AIAgentBackend.Models.Domain;

namespace SemanticKernel.AIAgentBackend.Services
{
    public interface IJwtService
    {
        string GenerateToken(User user);
        string GenerateRefreshToken();
        bool ValidateToken(string token, out string? userId);
    }
}