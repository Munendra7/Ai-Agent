using SemanticKernel.AIAgentBackend.Models.Domain;

namespace SemanticKernel.AIAgentBackend.Repositories
{
    public interface IChatService
    {
        public Task AddMessageAsync(string userId, string message, string sender);

        public Task<IEnumerable<ChatHistory>> GetMessagesAsync(string userId);
    }
}
