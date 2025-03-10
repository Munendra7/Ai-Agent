using SemanticKernel.AIAgentBackend.Models.Domain;
using System.Runtime.InteropServices;

namespace SemanticKernel.AIAgentBackend.Repositories.Interface
{
    public interface IChatHistoryService
    {
        public Task AddMessageAsync(ChatHistory chatHistory);

        public Task AddMessagesAsync(List<ChatHistory> chatHistories);

        public Task<IEnumerable<ChatHistory>> GetMessagesAsync(Guid sessionId, int lastChats);
    }
}
