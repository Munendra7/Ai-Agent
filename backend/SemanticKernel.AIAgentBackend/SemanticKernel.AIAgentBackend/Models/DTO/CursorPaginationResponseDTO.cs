namespace SemanticKernel.AIAgentBackend.Models.DTO
{
    public class CursorPaginationResponseDTO<T>
    {
        public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
        public string? NextCursor { get; set; }
        public string? PreviousCursor { get; set; }
        public bool HasMore { get; set; }
    }
}
