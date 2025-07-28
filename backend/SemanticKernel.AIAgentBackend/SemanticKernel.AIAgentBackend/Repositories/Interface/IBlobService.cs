namespace SemanticKernel.AIAgentBackend.Repositories.Interface
{
    public interface IBlobService
    {
        Task<string> UploadFileAsync(Stream fileStream, string fileName, string containerName);

        Task<(Stream Content, string Url)> DownloadFileAsync(string fileName, string containerName);

        Task<List<string>> ListFilesAsync(string containerName, string[]? extensions = null);

        public string GenerateSasUri(string blobName, string containerName, int expiryMinutes = 60);
    }
}