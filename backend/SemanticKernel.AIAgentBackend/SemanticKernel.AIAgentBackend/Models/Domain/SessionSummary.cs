using System.ComponentModel.DataAnnotations;

namespace SemanticKernel.AIAgentBackend.Models.Domain
{
    public class SessionSummary
    {
        [Key]
        public Guid SessionId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public Guid UserId { get; set; }

        public User User { get; set; } = null!;
        public ICollection<ChatHistory> ChatHistories { get; set; } = new List<ChatHistory>();
    }
}
