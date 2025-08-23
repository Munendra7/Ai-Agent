namespace SemanticKernel.AIAgentBackend.Models.Configuration
{
    public class MicrosoftOAuthSettings
    {
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string RedirectUri { get; set; } = string.Empty;
        public string TenantId { get; set; } = "common";
    }
}
