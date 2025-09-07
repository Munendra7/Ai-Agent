using SemanticKernel.AIAgentBackend.Models.Domain;

namespace SemanticKernel.AIAgentBackend.Repositories.Interface
{
    public interface IChatHistoryService
    {
        public Task AddMessageAsync(ChatHistory chatHistory);

        public Task AddMessagesAsync(List<ChatHistory> chatHistories);

        public Task<IEnumerable<ChatHistory>> GetMessagesAsync(Guid sessionId, Guid userId, int lastChats);

        public Task<IEnumerable<SessionSummary>> GetSessionSummariesAsync(Guid userId, int lastSessions);

        public Task<string> GetOrUpdateGroundingSummaryAsync(Guid sessionId, Guid userId, List<ChatHistory> chatHistories, string userQuery);
    }
}
