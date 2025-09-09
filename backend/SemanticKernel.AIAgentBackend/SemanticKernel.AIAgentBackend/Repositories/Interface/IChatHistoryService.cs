using SemanticKernel.AIAgentBackend.Models.Domain;
using SemanticKernel.AIAgentBackend.Models.DTO;

namespace SemanticKernel.AIAgentBackend.Repositories.Interface
{
    public interface IChatHistoryService
    {
        public Task AddMessageAsync(ChatHistory chatHistory, CancellationToken cancellationToken);

        public Task AddMessagesAsync(List<ChatHistory> chatHistories, CancellationToken cancellationToken);

        public Task<IEnumerable<ChatHistory>> GetMessagesAsync(Guid sessionId, Guid userId, int lastChats, CancellationToken cancellationToken);

        public Task<IEnumerable<SessionSummary>> GetSessionSummariesAsync(Guid userId, int lastSessions, CancellationToken cancellationToken);

        public Task<string> GetOrUpdateGroundingSummaryAsync(Guid sessionId, Guid userId, List<ChatHistory> chatHistories, string userQuery, CancellationToken cancellationToken);

        public Task<PaginationResponseDTO<ChatHistory>> GetPagedMessagesAsync(Guid sessionId, Guid userId, PaginationRequestDTO paginationRequestDTO, CancellationToken cancellationToken);

        public Task<PaginationResponseDTO<SessionSummary>> GetPagedSessionsAsync(Guid userId, PaginationRequestDTO paginationRequestDTO, CancellationToken cancellationToken);
    }
}
