using System.ComponentModel.DataAnnotations;

namespace SemanticKernel.AIAgentBackend.Models.DTO
{
    public class FileUploadDTO
    {
        [Required]
        public required IFormFile File { get; set; }

        [Required]
        public required string FileName { get; set; }

        public string? FileDescription { get; set; }

        [Required]
        [EnumDataType(typeof(AIModel), ErrorMessage = "Invalid model type. Allowed values: AzureOpenAI, Ollama, OpenAI")]
        public required string Model { get; set; }
    }
}
