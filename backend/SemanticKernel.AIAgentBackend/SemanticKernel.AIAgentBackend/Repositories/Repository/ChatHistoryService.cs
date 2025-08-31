
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using SemanticKernel.AIAgentBackend.Data;
using SemanticKernel.AIAgentBackend.Factories.Interface;
using SemanticKernel.AIAgentBackend.Models.Domain;
using SemanticKernel.AIAgentBackend.Repositories.Interface;
using ChatHistory = SemanticKernel.AIAgentBackend.Models.Domain.ChatHistory;

namespace SemanticKernel.AIAgentBackend.Repositories.Repository
{
    public class ChatHistoryService : IChatHistoryService
    {
        private readonly AppDbContext dbContext;
        private readonly Kernel _kernel;

        public ChatHistoryService(AppDbContext dbContext, [FromKeyedServices("LLMKernel")] Kernel kernel)
        {
            this.dbContext = dbContext;
            _kernel = kernel;
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

        public async Task<IEnumerable<ChatHistory>> GetMessagesAsync(Guid sessionId, Guid userId, int lastChats)
        {
            return await dbContext.ChatHistory
                .Where(x => x.SessionId == sessionId && x.UserId==userId)
                .OrderByDescending(x => x.Timestamp)
                .Take(lastChats)
                .OrderBy(x => x.Timestamp)
                .ToListAsync();
        }

        public async Task<IEnumerable<SessionSummary>> GetSessionSummariesAsync(Guid userId, int lastSessions)
        {
            return await dbContext.SessionSummaries
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.UpdatedAt)
                .Take(lastSessions)
                .ToListAsync();
        }

        public async Task<string> GetOrUpdateGroundingSummaryAsync(Guid sessionId, Guid userId, List<ChatHistory> chatHistories, string userQuery)
        {
            var summary = await dbContext.SessionSummaries.FirstOrDefaultAsync(s => s.SessionId == sessionId);

            if (summary != null && summary.UpdatedAt > DateTime.UtcNow.AddSeconds(-15))
                return summary.Content;

            string chatContent = string.Join("\n", chatHistories.Select(x => $"{x.Sender}: {x.Message}"));

            chatContent += $"\nUser: {userQuery}";

            string summarygroundingprompt = $@"
                You are an intelligent assistant designed to generate factual and context-aware chat summaries for future grounding.

                Your task is to create a brief and accurate summary of the following chat history between a user and assistant.

                ### Instructions:
                - Focus only on source of inforamtion, factual exchanges and meaningful questions or answers.
                - Do NOT add information that is not explicitly present in the chat.
                - Do NOT speculate, assume, or generalize beyond what was said.
                - Keep the summary clear, coherent, and within 100 words.
                - Preserve key topics, information source file, user intents, and assistant responses.
                - If the user is asking for info from specific file name, sheet name keep it always in your summary.

                ### Previous Chat Context:
                {summary?.Content}

                ### Chat History:
                {chatContent}

                ### Summary (Max 100 words):
                ";

            var result = await _kernel.InvokePromptAsync(summarygroundingprompt);

            string finalSummary = result?.GetValue<string>() ?? "No summary generated.";

            string summaryTitlePrompt = $@"
                Give me a Title for this chat summary in less than 30 characters.
                ### Chat Summary:
                {finalSummary}
                ### Title (Max 30 characters):
                ";

            var titleResult = await _kernel.InvokePromptAsync(summaryTitlePrompt);
            string summaryTitle = titleResult?.GetValue<string>() ?? chatHistories.First().Message ?? string.Empty;

            if (summary == null)
            {
                summary = new SessionSummary
                {
                    SessionId = sessionId,
                    UserId = userId,
                    Content = finalSummary,
                    Title = summaryTitle,
                    UpdatedAt = DateTime.UtcNow
                };
                dbContext.SessionSummaries.Add(summary);
            }
            else
            {
                summary.Content = finalSummary;
                summary.Title = summaryTitle;
                summary.UpdatedAt = DateTime.UtcNow;
            }

            await dbContext.SaveChangesAsync();
            return summary.Content;
        }
    }
}
