namespace SemanticKernel.AIAgentBackend.Models.DTO
{
    public class CursorPaginationRequestDTO
    {
        public string? Cursor { get; set; }
        public int PageSize { get; set; } = 20;
        public bool IsNext { get; set; } = true;
    }
}
