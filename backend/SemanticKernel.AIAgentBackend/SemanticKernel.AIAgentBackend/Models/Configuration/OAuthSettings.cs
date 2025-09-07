namespace SemanticKernel.AIAgentBackend.Models.Configuration
{
    public class OAuthSettings
    {
        public GoogleOAuthSettings Google { get; set; } = new();
        public MicrosoftOAuthSettings Microsoft { get; set; } = new();
    }
}
