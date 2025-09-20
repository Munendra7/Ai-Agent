
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using SemanticKernel.AIAgentBackend.Data;
using SemanticKernel.AIAgentBackend.Factories.Interface;
using SemanticKernel.AIAgentBackend.Models.Domain;
using SemanticKernel.AIAgentBackend.Models.DTO;
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

        public async Task AddMessageAsync(ChatHistory chatHistory, CancellationToken cancellationToken)
        {
            dbContext.ChatHistory.Add(chatHistory);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task AddMessagesAsync(List<ChatHistory> chatHistories, CancellationToken cancellationToken)
        {
            dbContext.ChatHistory.AddRange(chatHistories);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task<IEnumerable<ChatHistory>> GetMessagesAsync(Guid sessionId, Guid userId, int lastChats, CancellationToken cancellationToken)
        {
            return await dbContext.ChatHistory
                .Where(x => x.SessionId == sessionId && x.UserId==userId)
                .OrderByDescending(x => x.Timestamp)
                .Take(lastChats)
                .OrderBy(x => x.Timestamp)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<SessionSummary>> GetSessionSummariesAsync(Guid userId, int lastSessions, CancellationToken cancellationToken)
        {
            return await dbContext.SessionSummaries
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.UpdatedAt)
                .Take(lastSessions)
                .ToListAsync(cancellationToken);
        }

        public async Task<string> GetOrUpdateGroundingSummaryAsync(Guid sessionId, Guid userId, List<ChatHistory> chatHistories, string userQuery, CancellationToken cancellationToken)
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

            var result = await _kernel.InvokePromptAsync(summarygroundingprompt, null, null, null, cancellationToken);

            string finalSummary = result?.GetValue<string>() ?? "No summary generated.";

            string summaryTitlePrompt = $@"
                Give me a Title for this chat summary in less than 30 characters.
                ### Chat Summary:
                {finalSummary}
                ### Title (Max 30 characters):
                ";

            var titleResult = await _kernel.InvokePromptAsync(summaryTitlePrompt, null, null, null, cancellationToken);
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

            await dbContext.SaveChangesAsync(cancellationToken);
            return summary.Content;
        }

        public async Task<PaginationResponseDTO<ChatHistory>> GetPagedMessagesAsync(Guid sessionId, Guid userId, PaginationRequestDTO paginationRequestDTO, CancellationToken cancellationToken)
        {
            var totalCount = await dbContext.ChatHistory.CountAsync(x => x.SessionId == sessionId && x.UserId == userId, cancellationToken);

            var items = await dbContext.ChatHistory
                .Where(x => x.SessionId == sessionId && x.UserId == userId)
                .OrderBy(x => x.Timestamp)
                .Skip((paginationRequestDTO.PageNumber - 1) * paginationRequestDTO.PageSize)
                .Take(paginationRequestDTO.PageSize)
                .ToListAsync(cancellationToken);

            return new PaginationResponseDTO<ChatHistory> {
                Items = items,
                PageNumber = paginationRequestDTO.PageNumber,
                PageSize = paginationRequestDTO.PageSize,
                TotalCount = totalCount
            };
        }

        public async Task<PaginationResponseDTO<SessionSummary>> GetPagedSessionsAsync(Guid userId, PaginationRequestDTO paginationRequestDTO, CancellationToken cancellationToken)
        {
            var totalCount = await dbContext.SessionSummaries.CountAsync(s => s.UserId == userId, cancellationToken);

            var items = await dbContext.SessionSummaries
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.UpdatedAt)
                .Skip((paginationRequestDTO.PageNumber - 1) * paginationRequestDTO.PageSize)
                .Take(paginationRequestDTO.PageSize)
                .ToListAsync(cancellationToken);

            return new PaginationResponseDTO<SessionSummary>
            {
                Items = items,
                PageNumber = paginationRequestDTO.PageNumber,
                PageSize = paginationRequestDTO.PageSize,
                TotalCount = totalCount
            };
        }

        //Cursor based pagination implementation

        public async Task<CursorPaginationResponseDTO<ChatHistory>> GetCursorMessagesAsync(Guid sessionId, Guid userId, CursorPaginationRequestDTO request, CancellationToken cancellationToken)
        {
            // Use Timestamp as cursor
            DateTime? cursorTime = null;
            if (!string.IsNullOrEmpty(request.Cursor) && DateTime.TryParse(request.Cursor, out var parsed))
                cursorTime = parsed;

            var query = dbContext.ChatHistory
                .Where(x => x.SessionId == sessionId && x.UserId == userId);

            if (cursorTime.HasValue)
            {
                if (request.IsNext)
                    query = query.Where(x => x.Timestamp > cursorTime.Value);
                else
                    query = query.Where(x => x.Timestamp < cursorTime.Value);
            }

            query = request.IsNext
                ? query.OrderBy(x => x.Timestamp)
                : query.OrderByDescending(x => x.Timestamp);

            var items = await query
                .Take(request.PageSize + 1) // fetch one extra to detect HasMore
                .ToListAsync(cancellationToken);

            var hasMore = items.Count > request.PageSize;
            if (hasMore) items = items.Take(request.PageSize).ToList();

            // Get new cursors
            string? nextCursor = items.Any() ? items.Last().Timestamp.ToString("O") : null;
            string? prevCursor = items.Any() ? items.First().Timestamp.ToString("O") : null;

            // Always return items in ascending order for UI consistency
            items = items.OrderBy(x => x.Timestamp).ToList();

            return new CursorPaginationResponseDTO<ChatHistory>
            {
                Items = items,
                NextCursor = nextCursor,
                PreviousCursor = prevCursor,
                HasMore = hasMore
            };
        }

        public async Task<CursorPaginationResponseDTO<SessionSummary>> GetCursorSessionsAsync(Guid userId, CursorPaginationRequestDTO request, CancellationToken cancellationToken)
        {
            DateTime? cursorTime = null;
            if (!string.IsNullOrEmpty(request.Cursor) && DateTime.TryParse(request.Cursor, out var parsed))
                cursorTime = parsed;

            var query = dbContext.SessionSummaries
                .Where(s => s.UserId == userId);

            if (cursorTime.HasValue)
            {
                if (request.IsNext)
                    query = query.Where(s => s.UpdatedAt < cursorTime.Value); // sessions are newest first
                else
                    query = query.Where(s => s.UpdatedAt > cursorTime.Value);
            }

            query = request.IsNext
                ? query.OrderByDescending(s => s.UpdatedAt)
                : query.OrderBy(s => s.UpdatedAt);

            var items = await query
                .Take(request.PageSize + 1)
                .ToListAsync(cancellationToken);

            var hasMore = items.Count > request.PageSize;
            if (hasMore) items = items.Take(request.PageSize).ToList();

            string? nextCursor = items.Any() ? items.Last().UpdatedAt.ToString("O") : null;
            string? prevCursor = items.Any() ? items.First().UpdatedAt.ToString("O") : null;

            return new CursorPaginationResponseDTO<SessionSummary>
            {
                Items = items,
                NextCursor = nextCursor,
                PreviousCursor = prevCursor,
                HasMore = hasMore
            };
        }
    }
}
