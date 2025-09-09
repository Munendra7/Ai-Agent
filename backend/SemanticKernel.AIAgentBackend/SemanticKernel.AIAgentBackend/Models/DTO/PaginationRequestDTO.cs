namespace SemanticKernel.AIAgentBackend.Models.DTO
{
    public class PaginationRequestDTO
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
