namespace SemanticKernel.AIAgentBackend.Models.Domain
{
    public class ChatHistory
    {
        public Guid Id { get; set; }

        public Guid SessionId { get; set; }
        public SessionSummary SessionSummary { get; set; } = null!;

        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public string? Message { get; set; }

        public string? Sender { get; set; }

        public DateTime Timestamp { get; set; }
    }
}
