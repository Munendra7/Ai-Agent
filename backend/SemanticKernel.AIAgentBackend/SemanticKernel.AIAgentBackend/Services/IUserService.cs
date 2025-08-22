using SemanticKernel.AIAgentBackend.Models.Domain;
using SemanticKernel.AIAgentBackend.Models.DTO;

namespace SemanticKernel.AIAgentBackend.Services
{
    public interface IUserService
    {
        Task<User?> GetUserByProviderAsync(string provider, string providerId);
        Task<User> CreateUserAsync(string email, string name, string provider, string providerId, string? profilePictureUrl = null);
        Task<User?> GetUserByIdAsync(Guid userId);
        Task<User> UpdateLastLoginAsync(User user);
        Task<UserDto> MapToUserDto(User user);
        Task AssignDefaultRoleAsync(User user);
    }
}