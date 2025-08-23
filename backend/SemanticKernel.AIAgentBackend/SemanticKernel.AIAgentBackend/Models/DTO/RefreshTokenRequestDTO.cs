using System.ComponentModel.DataAnnotations;

namespace SemanticKernel.AIAgentBackend.Models.DTO
{
    public class RefreshTokenRequestDTO
    {
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
