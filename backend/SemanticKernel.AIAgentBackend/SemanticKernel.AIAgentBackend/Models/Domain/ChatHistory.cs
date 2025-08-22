namespace SemanticKernel.AIAgentBackend.Models.Domain
{
    public class ChatHistory
    {
        public Guid Id { get; set; }

        public Guid SessionId { get; set; }

        public Guid UserId { get; set; }

        public string? Message { get; set; }

        public string? Sender { get; set; }

        public DateTime Timestamp { get; set; }
    }
}
