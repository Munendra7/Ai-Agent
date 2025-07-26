using SemanticKernel.AIAgentBackend.Models.Domain;

namespace SemanticKernel.AIAgentBackend.Repositories.Interface
{
    public interface IChatHistoryService
    {
        public Task AddMessageAsync(ChatHistory chatHistory);

        public Task AddMessagesAsync(List<ChatHistory> chatHistories);

        public Task<IEnumerable<ChatHistory>> GetMessagesAsync(Guid sessionId, int lastChats);

        public Task<string> GetOrUpdateGroundingSummaryAsync(Guid sessionId, List<ChatHistory> chatHistories);
    }
}
