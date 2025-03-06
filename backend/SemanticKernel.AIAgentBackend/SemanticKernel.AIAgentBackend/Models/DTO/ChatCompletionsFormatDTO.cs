namespace SemanticKernel.AIAgentBackend.Models.DTO
{
    public class ChatCompletionsFormatDTO
    {
        public string userQuery { get; set; }
        public string assistantResponse { get; set; }

        public ChatCompletionsFormatDTO(string userQuery, string assistantResponse)
        {
            this.userQuery = userQuery;
            this.assistantResponse = assistantResponse;
        }
    }
}
