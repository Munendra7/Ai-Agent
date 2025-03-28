using System.ComponentModel.DataAnnotations;

namespace SemanticKernel.AIAgentBackend.Models.DTO
{
    public class TemplateUploadDTO
    {
        [Required]
        public required IFormFile File { get; set; }
    }
}
