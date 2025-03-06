
using Microsoft.EntityFrameworkCore;
using SemanticKernel.AIAgentBackend.Data;
using SemanticKernel.AIAgentBackend.Models.Domain;

namespace SemanticKernel.AIAgentBackend.Repositories
{
    public class ChatService : IChatService
    {
        private readonly AppDbContext dbContext;

        public ChatService(AppDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task AddMessageAsync(string userId, string message, string sender)
        {
            var chatHistory = new ChatHistory
            {
                UserId = userId,
                Message = message,
                Sender = sender,
                Timestamp = DateTime.UtcNow
            };

            dbContext.ChatHistory.Add(chatHistory);
            await dbContext.SaveChangesAsync();
        }

        public async Task<IEnumerable<ChatHistory>> GetMessagesAsync(string userId)
        {
            return await dbContext.ChatHistory
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.Timestamp)
                .Take(10).OrderBy(x => x.Timestamp)
                .ToListAsync();
        }
    }
}
