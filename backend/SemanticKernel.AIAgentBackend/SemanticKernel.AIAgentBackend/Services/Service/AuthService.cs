using Microsoft.AspNetCore.Identity;
using SemanticKernel.AIAgentBackend.Models.Domain;
using SemanticKernel.AIAgentBackend.Models.DTO;
using SemanticKernel.AIAgentBackend.Data;
using Microsoft.EntityFrameworkCore;
using SemanticKernel.AIAgentBackend.Services.Interface;
using SemanticKernel.AIAgentBackend.Services.Model;

namespace SemanticKernel.AIAgentBackend.Services.Service
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly ITokenService _tokenService;
        private readonly IPasswordHasher<User> _passwordHasher;

        public AuthService(AppDbContext context, ITokenService tokenService, IPasswordHasher<User> passwordHasher)
        {
            _context = context;
            _tokenService = tokenService;
            _passwordHasher = passwordHasher;
        }

        public async Task<AuthResponseDTO?> RegisterAsync(RegisterRequestDTO request, string ipAddress)
        {
            // Check if user exists
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                return null;

            // Create user
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                CreatedAt = DateTime.UtcNow,
                Provider = "local"
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

            var userRole = await _context.Roles.FirstAsync(r => r.Name == "User");
            user.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = userRole.Id });

            // Generate tokens
            var refreshToken = _tokenService.GenerateRefreshToken(ipAddress);
            user.RefreshTokens.Add(refreshToken);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var roles = new[] { userRole.Name };
            var accessToken = _tokenService.GenerateAccessToken(user, roles);

            return new AuthResponseDTO
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken.Token,
                User = MapToUserDto(user, roles)
            };
        }

        public async Task<AuthResponseDTO?> LoginAsync(LoginRequestDTO request, string ipAddress)
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Include(u => u.RefreshTokens)
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null || string.IsNullOrEmpty(user.PasswordHash))
                return null;

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
            if (result != PasswordVerificationResult.Success)
                return null;

            // Rotate refresh token
            var refreshToken = _tokenService.GenerateRefreshToken(ipAddress);
            user.RefreshTokens.Add(refreshToken);

            // Remove old inactive tokens
            RemoveOldRefreshTokens(user);

            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
            var accessToken = _tokenService.GenerateAccessToken(user, roles);

            return new AuthResponseDTO
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken.Token,
                User = MapToUserDto(user, roles)
            };
        }

        public async Task<AuthResponseDTO?> RefreshTokenAsync(string token, string ipAddress)
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Include(u => u.RefreshTokens)
                .FirstOrDefaultAsync(u => u.RefreshTokens.Any(t => t.Token == token));

            if (user == null)
                return null;

            var refreshToken = user.RefreshTokens.Single(x => x.Token == token);

            if (!refreshToken.IsActive)
                return null;

            // Rotate refresh token
            var newRefreshToken = _tokenService.GenerateRefreshToken(ipAddress);
            refreshToken.RevokedAt = DateTime.UtcNow;
            refreshToken.RevokedByIp = ipAddress;
            refreshToken.ReplacedByToken = newRefreshToken.Token;

            user.RefreshTokens.Add(newRefreshToken);
            RemoveOldRefreshTokens(user);

            await _context.SaveChangesAsync();

            var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
            var accessToken = _tokenService.GenerateAccessToken(user, roles);

            return new AuthResponseDTO
            {
                AccessToken = accessToken,
                RefreshToken = newRefreshToken.Token,
                User = MapToUserDto(user, roles)
            };
        }

        public async Task<AuthResponseDTO?> ExternalLoginAsync(string provider, OAuthUserInfo userInfo, string ipAddress)
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Include(u => u.RefreshTokens)
                .FirstOrDefaultAsync(u => u.Email == userInfo.Email);

            if (user == null)
            {
                // Create new user
                user = new User
                {
                    Id = Guid.NewGuid(),
                    Email = userInfo.Email,
                    FirstName = userInfo.FirstName,
                    LastName = userInfo.LastName,
                    Provider = provider,
                    ProviderId = userInfo.Id,
                    CreatedAt = DateTime.UtcNow
                };

                var userRole = await _context.Roles.FirstAsync(r => r.Name == "User");
                user.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = userRole.Id });

                _context.Users.Add(user);
            }
            else
            {
                // Update provider info if needed
                if (string.IsNullOrEmpty(user.Provider))
                {
                    user.Provider = provider;
                    user.ProviderId = userInfo.Id;
                }
            }

            // Generate tokens
            var refreshToken = _tokenService.GenerateRefreshToken(ipAddress);
            user.RefreshTokens.Add(refreshToken);
            RemoveOldRefreshTokens(user);

            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
            var accessToken = _tokenService.GenerateAccessToken(user, roles);

            return new AuthResponseDTO
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken.Token,
                User = MapToUserDto(user, roles)
            };
        }

        public async Task RevokeTokenAsync(string token, string ipAddress)
        {
            var user = await _context.Users
                .Include(u => u.RefreshTokens)
                .FirstOrDefaultAsync(u => u.RefreshTokens.Any(t => t.Token == token));

            if (user == null)
                return;

            var refreshToken = user.RefreshTokens.Single(x => x.Token == token);

            if (refreshToken.IsActive)
            {
                refreshToken.RevokedAt = DateTime.UtcNow;
                refreshToken.RevokedByIp = ipAddress;
                await _context.SaveChangesAsync();
            }
        }

        private void RemoveOldRefreshTokens(User user)
        {
            // Convert ICollection to List to use RemoveAll  
            var refreshTokens = user.RefreshTokens.ToList();

            // Remove old inactive refresh tokens from user based on TTL in app settings  
            refreshTokens.RemoveAll(x =>
                !x.IsActive &&
                x.CreatedAt.AddDays(7) <= DateTime.UtcNow);

            // Update the original collection  
            user.RefreshTokens = refreshTokens;
        }

        private UserDTO MapToUserDto(User user, IEnumerable<string> roles)
        {
            return new UserDTO
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Roles = roles.ToList()
            };
        }
    }
}
