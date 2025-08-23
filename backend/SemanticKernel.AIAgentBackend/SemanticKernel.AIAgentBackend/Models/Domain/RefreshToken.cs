namespace SemanticKernel.AIAgentBackend.Models.Domain
{
    public class RefreshToken
    {
        public Guid Id { get; set; }
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? RevokedAt { get; set; }
        public string? ReplacedByToken { get; set; }
        public string CreatedByIp { get; set; } = string.Empty;
        public string? RevokedByIp { get; set; }

        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public bool IsActive => RevokedAt == null && DateTime.UtcNow < ExpiresAt;
    }
}
