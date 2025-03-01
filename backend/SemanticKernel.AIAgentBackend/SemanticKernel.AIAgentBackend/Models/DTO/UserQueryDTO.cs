using System.ComponentModel.DataAnnotations;

namespace SemanticKernel.AIAgentBackend.Models.DTO
{
    public class UserQueryDTO
    {
        [Required]
        [MaxLength(500, ErrorMessage = "Prompt cannot be greater than 500 characters")]
        public required string Query { get; set; }

        [Required]
        [EnumDataType(typeof(AIModel), ErrorMessage = "Invalid model type. Allowed values: AzureOpenAI, Ollama, OpenAI")]
        public required string Model { get; set; }
    }

    public enum AIModel
    {
        AzureOpenAI,
        Ollama,
        OpenAI
    }
}