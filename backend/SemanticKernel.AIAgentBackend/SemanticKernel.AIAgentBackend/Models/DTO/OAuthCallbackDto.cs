namespace SemanticKernel.AIAgentBackend.Models.DTO
{
    public class OAuthCallbackDto
    {
        public string Code { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string? Error { get; set; }
        public string? ErrorDescription { get; set; }
    }
}