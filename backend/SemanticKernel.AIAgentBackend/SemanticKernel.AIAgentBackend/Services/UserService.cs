using Microsoft.EntityFrameworkCore;
using SemanticKernel.AIAgentBackend.Data;
using SemanticKernel.AIAgentBackend.Models.Domain;
using SemanticKernel.AIAgentBackend.Models.DTO;

namespace SemanticKernel.AIAgentBackend.Services
{
    public class UserService : IUserService
    {
        private readonly AppDbContext _context;

        public UserService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetUserByProviderAsync(string provider, string providerId)
        {
            return await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Provider == provider && u.ProviderId == providerId);
        }

        public async Task<User> CreateUserAsync(string email, string name, string provider, string providerId, string? profilePictureUrl = null)
        {
            var user = new User
            {
                Email = email,
                Name = name,
                Provider = provider,
                ProviderId = providerId,
                ProfilePictureUrl = profilePictureUrl,
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Assign default role
            await AssignDefaultRoleAsync(user);

            // Reload user with roles
            return await GetUserByProviderAsync(provider, providerId) ?? user;
        }

        public async Task<User?> GetUserByIdAsync(Guid userId)
        {
            return await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<User> UpdateLastLoginAsync(User user)
        {
            user.LastLoginAt = DateTime.UtcNow;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<UserDto> MapToUserDto(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                Name = user.Name,
                ProfilePictureUrl = user.ProfilePictureUrl,
                Provider = user.Provider,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt,
                Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList()
            };
        }

        public async Task AssignDefaultRoleAsync(User user)
        {
            var defaultRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "User");
            if (defaultRole != null)
            {
                var userRole = new UserRole
                {
                    UserId = user.Id,
                    RoleId = defaultRole.Id
                };

                _context.UserRoles.Add(userRole);
                await _context.SaveChangesAsync();
            }
        }
    }
}