using System.ComponentModel.DataAnnotations;

namespace SemanticKernel.AIAgentBackend.Models.Domain
{
    public class RefreshToken
    {
        public Guid Id { get; set; }
        
        [Required]
        public Guid UserId { get; set; }
        
        [Required]
        [MaxLength(500)]
        public string Token { get; set; } = string.Empty;
        
        public DateTime ExpiresAt { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public bool IsRevoked { get; set; } = false;
        
        // Navigation property
        public virtual User User { get; set; } = null!;
    }
}