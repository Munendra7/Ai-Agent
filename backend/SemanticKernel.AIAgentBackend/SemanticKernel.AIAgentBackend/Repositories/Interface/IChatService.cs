using SemanticKernel.AIAgentBackend.Models.Domain;
using System.Runtime.InteropServices;

namespace SemanticKernel.AIAgentBackend.Repositories.Interface
{
    public interface IChatService
    {
        public Task AddMessageAsync(ChatHistory chatHistory);

        public Task AddKernelPlannarLogsAsync(KernelPlannarLogs kernelPlannarLogs);

        public Task AddMessagesAsync(List<ChatHistory> chatHistories);

        public Task<IEnumerable<ChatHistory>> GetMessagesAsync(Guid sessionId, int lastChats);
    }
}
