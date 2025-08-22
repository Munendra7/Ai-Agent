using System.ComponentModel.DataAnnotations;

namespace SemanticKernel.AIAgentBackend.Models.Domain
{
    public class User
    {
        public Guid Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;
        
        [MaxLength(100)]
        public string? FirstName { get; set; }
        
        [MaxLength(100)]
        public string? LastName { get; set; }
        
        [MaxLength(255)]
        public string? ProfilePictureUrl { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string Provider { get; set; } = string.Empty; // "Google", "Microsoft", "GitHub"
        
        [Required]
        [MaxLength(100)]
        public string ProviderId { get; set; } = string.Empty; // External provider's user ID
        
        [Required]
        [MaxLength(20)]
        public string Role { get; set; } = "User"; // "Admin", "User", "Moderator"
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime LastLoginAt { get; set; } = DateTime.UtcNow;
        
        public bool IsActive { get; set; } = true;
        
        // Navigation properties
        public virtual ICollection<ChatHistory> ChatHistories { get; set; } = new List<ChatHistory>();
        public virtual ICollection<SessionSummary> SessionSummaries { get; set; } = new List<SessionSummary>();
    }
}