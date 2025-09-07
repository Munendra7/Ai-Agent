namespace SemanticKernel.AIAgentBackend.Models.DTO
{
    public class AuthResponseDTO
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public UserDTO User { get; set; } = null!;
    }
}
