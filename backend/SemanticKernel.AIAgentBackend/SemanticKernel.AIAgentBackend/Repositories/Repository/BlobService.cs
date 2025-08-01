﻿using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs;
using SemanticKernel.AIAgentBackend.Repositories.Interface;
using Microsoft.AspNetCore.StaticFiles;
using Azure.Storage.Sas;

namespace SemanticKernel.AIAgentBackend.Repositories.Repository
{
    public class BlobService : IBlobService
    {
        private readonly BlobServiceClient _blobServiceClient;

        public BlobService(BlobServiceClient blobServiceClient)
        {
            _blobServiceClient = blobServiceClient;
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string containerName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);
            var blobClient = containerClient.GetBlobClient(fileName);

            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(fileName, out string? contentType))
            {
                contentType = "application/octet-stream"; // Default if type not found
            }

            var blobHttpHeaders = new BlobHttpHeaders { ContentType = contentType };
            await blobClient.UploadAsync(fileStream, blobHttpHeaders);

            return blobClient.Uri.ToString();
        }

        public async Task<(Stream Content, string Url)> DownloadFileAsync(string fileName, string containerName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);
            var blobClient = containerClient.GetBlobClient(fileName);
            var response = await blobClient.DownloadAsync();

            return (response.Value.Content, blobClient.Uri.ToString());
        }

        public async Task<List<string>> ListFilesAsync(string containerName, string[]? extensions = null)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

            List<string> fileNames = new List<string>();

            await foreach (BlobItem blobItem in containerClient.GetBlobsAsync())
            {
                if (extensions == null || extensions.Length == 0 || extensions.Any(ext => blobItem.Name.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
                {
                    fileNames.Add(blobItem.Name);
                }
            }

            return fileNames;
        }

        public string GenerateSasUri(string blobName, string containerName, int expiryMinutes = 60)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            if (!blobClient.CanGenerateSasUri)
            {
                throw new InvalidOperationException("SAS generation not supported for this blob client. Ensure the client is initialized with StorageSharedKeyCredential.");
            }

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = containerName,
                BlobName = blobName,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(expiryMinutes)
            };

            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            var sasUri = blobClient.GenerateSasUri(sasBuilder);
            return sasUri.ToString();
        }
    }
}
