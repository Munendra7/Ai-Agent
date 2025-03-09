namespace SemanticKernel.AIAgentBackend.Models.Domain
{
    public class KernelPlannarLogs
    {
        public Guid Id { get; set; }

        public string? PlannarText { get; set; }

        public string? Exception { get; set; }

        public DateTime Timestamp { get; set; }
    }
}
