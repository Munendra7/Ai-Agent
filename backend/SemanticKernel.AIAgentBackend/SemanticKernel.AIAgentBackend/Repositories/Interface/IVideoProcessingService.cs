namespace SemanticKernel.AIAgentBackend.Repositories.Interface
{
    public interface IVideoProcessingService
    {
        public Task<IEnumerable<string>> ProcessVideo(string fileName);
    }
}
