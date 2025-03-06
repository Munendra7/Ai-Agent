using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace SemanticKernel.AIAgentBackend.Models.Domain
{
    public class ChatHistory
    {
        public Guid Id { get; set; }

        public string? UserId { get; set; }

        public string? Message { get; set; }

        public string? Sender { get; set; }

        public DateTime Timestamp { get; set; }
    }
}
