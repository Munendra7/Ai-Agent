using Microsoft.AspNetCore.Identity;

namespace SemanticKernel.AIAgentBackend.Models.Domain
{
    public class ApplicationRole : IdentityRole
    {
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
