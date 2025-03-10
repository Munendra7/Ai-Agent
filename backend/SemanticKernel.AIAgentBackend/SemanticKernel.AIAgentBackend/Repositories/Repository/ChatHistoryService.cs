
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel.ChatCompletion;
using SemanticKernel.AIAgentBackend.Data;
using SemanticKernel.AIAgentBackend.Models.Domain;
using SemanticKernel.AIAgentBackend.Repositories.Interface;
using ChatHistory = SemanticKernel.AIAgentBackend.Models.Domain.ChatHistory;

namespace SemanticKernel.AIAgentBackend.Repositories.Repository
{
    public class ChatHistoryService : IChatHistoryService
    {
        private readonly AppDbContext dbContext;

        public ChatHistoryService(AppDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task AddMessageAsync(ChatHistory chatHistory)
        {
            dbContext.ChatHistory.Add(chatHistory);
            await dbContext.SaveChangesAsync();
        }

        public async Task AddMessagesAsync(List<ChatHistory> chatHistories)
        {
            dbContext.ChatHistory.AddRange(chatHistories);
            await dbContext.SaveChangesAsync();
        }

        public async Task<IEnumerable<ChatHistory>> GetMessagesAsync(Guid sessionId, int lastChats)
        {
            return await dbContext.ChatHistory
                .Where(x => x.SessionId == sessionId)
                .OrderByDescending(x => x.Timestamp)
                .Take(lastChats)
                .OrderBy(x => x.Timestamp)
                .ToListAsync();
        }
    }
}
