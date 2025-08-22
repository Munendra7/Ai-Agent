using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SemanticKernel.AIAgentBackend.Data;
using SemanticKernel.AIAgentBackend.Models.Domain;
using SemanticKernel.AIAgentBackend.Models.DTO;
using SemanticKernel.AIAgentBackend.Services;

namespace SemanticKernel.AIAgentBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IExternalAuthService _externalAuthService;
        private readonly IJwtService _jwtService;

        public AuthController(AppDbContext context, IExternalAuthService externalAuthService, IJwtService jwtService)
        {
            _context = context;
            _externalAuthService = externalAuthService;
            _jwtService = jwtService;
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
        {
            try
            {
                // Validate external token
                ExternalUserInfo? userInfo = request.Provider.ToLower() switch
                {
                    "google" => await _externalAuthService.ValidateGoogleTokenAsync(request.AccessToken),
                    "microsoft" => await _externalAuthService.ValidateMicrosoftTokenAsync(request.AccessToken),
                    "github" => await _externalAuthService.ValidateGitHubTokenAsync(request.AccessToken),
                    _ => null
                };

                if (userInfo == null)
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                // Find or create user
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Provider == userInfo.Provider && u.ProviderId == userInfo.Id);

                if (user == null)
                {
                    // Check if user exists with same email but different provider
                    var existingUser = await _context.Users
                        .FirstOrDefaultAsync(u => u.Email == userInfo.Email);

                    if (existingUser != null)
                    {
                        return BadRequest(new { message = "User with this email already exists with a different provider" });
                    }

                    // Create new user
                    user = new User
                    {
                        Email = userInfo.Email,
                        FirstName = userInfo.FirstName,
                        LastName = userInfo.LastName,
                        ProfilePictureUrl = userInfo.ProfilePictureUrl,
                        Provider = userInfo.Provider,
                        ProviderId = userInfo.Id,
                        Role = "User", // Default role
                        CreatedAt = DateTime.UtcNow,
                        LastLoginAt = DateTime.UtcNow
                    };

                    _context.Users.Add(user);
                }
                else
                {
                    // Update last login
                    user.LastLoginAt = DateTime.UtcNow;
                    user.FirstName = userInfo.FirstName ?? user.FirstName;
                    user.LastName = userInfo.LastName ?? user.LastName;
                    user.ProfilePictureUrl = userInfo.ProfilePictureUrl ?? user.ProfilePictureUrl;
                }

                // Generate tokens
                var token = _jwtService.GenerateToken(user);
                var refreshToken = _jwtService.GenerateRefreshToken();

                // Save refresh token
                var refreshTokenEntity = new RefreshToken
                {
                    UserId = user.Id,
                    Token = refreshToken,
                    ExpiresAt = DateTime.UtcNow.AddDays(7), // Refresh token expires in 7 days
                    CreatedAt = DateTime.UtcNow
                };

                _context.RefreshTokens.Add(refreshTokenEntity);
                await _context.SaveChangesAsync();

                return Ok(new AuthResponse
                {
                    Token = token,
                    RefreshToken = refreshToken,
                    ExpiresAt = DateTime.UtcNow.AddHours(1),
                    User = new UserDto
                    {
                        Id = user.Id,
                        Email = user.Email,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        ProfilePictureUrl = user.ProfilePictureUrl,
                        Provider = user.Provider,
                        Role = user.Role,
                        CreatedAt = user.CreatedAt,
                        LastLoginAt = user.LastLoginAt
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        [HttpPost("refresh")]
        public async Task<ActionResult<AuthResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                var refreshToken = await _context.RefreshTokens
                    .Include(rt => rt.User)
                    .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken && !rt.IsRevoked);

                if (refreshToken == null || refreshToken.ExpiresAt < DateTime.UtcNow)
                {
                    return Unauthorized(new { message = "Invalid or expired refresh token" });
                }

                // Generate new tokens
                var newToken = _jwtService.GenerateToken(refreshToken.User);
                var newRefreshToken = _jwtService.GenerateRefreshToken();

                // Revoke old refresh token
                refreshToken.IsRevoked = true;

                // Save new refresh token
                var newRefreshTokenEntity = new RefreshToken
                {
                    UserId = refreshToken.UserId,
                    Token = newRefreshToken,
                    ExpiresAt = DateTime.UtcNow.AddDays(7),
                    CreatedAt = DateTime.UtcNow
                };

                _context.RefreshTokens.Add(newRefreshTokenEntity);
                await _context.SaveChangesAsync();

                return Ok(new AuthResponse
                {
                    Token = newToken,
                    RefreshToken = newRefreshToken,
                    ExpiresAt = DateTime.UtcNow.AddHours(1),
                    User = new UserDto
                    {
                        Id = refreshToken.User.Id,
                        Email = refreshToken.User.Email,
                        FirstName = refreshToken.User.FirstName,
                        LastName = refreshToken.User.LastName,
                        ProfilePictureUrl = refreshToken.User.ProfilePictureUrl,
                        Provider = refreshToken.User.Provider,
                        Role = refreshToken.User.Role,
                        CreatedAt = refreshToken.User.CreatedAt,
                        LastLoginAt = refreshToken.User.LastLoginAt
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<ActionResult> Logout()
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                {
                    return Unauthorized();
                }

                // Revoke all refresh tokens for the user
                var refreshTokens = await _context.RefreshTokens
                    .Where(rt => rt.UserId.ToString() == userId && !rt.IsRevoked)
                    .ToListAsync();

                foreach (var token in refreshTokens)
                {
                    token.IsRevoked = true;
                }

                await _context.SaveChangesAsync();

                return Ok(new { message = "Logged out successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<UserDto>> GetCurrentUser()
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                {
                    return Unauthorized();
                }

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id.ToString() == userId);

                if (user == null)
                {
                    return NotFound();
                }

                return Ok(new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    ProfilePictureUrl = user.ProfilePictureUrl,
                    Provider = user.Provider,
                    Role = user.Role,
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }
    }
}