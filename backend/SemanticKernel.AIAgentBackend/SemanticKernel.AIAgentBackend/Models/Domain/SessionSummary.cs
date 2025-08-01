﻿using System.ComponentModel.DataAnnotations;

namespace SemanticKernel.AIAgentBackend.Models.Domain
{
    public class SessionSummary
    {
        [Key]
        public Guid SessionId { get; set; }

        public string Content { get; set; } = string.Empty;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
