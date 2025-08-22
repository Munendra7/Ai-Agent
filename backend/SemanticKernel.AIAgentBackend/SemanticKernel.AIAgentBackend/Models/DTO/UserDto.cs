namespace SemanticKernel.AIAgentBackend.Models.DTO
{
    public class UserDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }
        public string Provider { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime LastLoginAt { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
    }
}