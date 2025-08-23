using System.ComponentModel.DataAnnotations;

namespace SemanticKernel.AIAgentBackend.Models.DTO
{
    public class ExternalAuthRequestDTO
    {
        [Required]
        public string Code { get; set; } = string.Empty;

        [Required]
        public string RedirectUri { get; set; } = string.Empty;
    }
}
