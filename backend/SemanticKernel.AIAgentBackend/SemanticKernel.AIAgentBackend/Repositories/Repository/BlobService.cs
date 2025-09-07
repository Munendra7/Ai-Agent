using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs;
using SemanticKernel.AIAgentBackend.Repositories.Interface;
using Microsoft.AspNetCore.StaticFiles;
using Azure.Storage.Sas;
using SemanticKernel.AIAgentBackend.Services.Interface;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace SemanticKernel.AIAgentBackend.Repositories.Repository
{
    public class BlobService : IBlobService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly IAuthService _authService;
        private const string USER_ID_METADATA_KEY = "UserId";
        private const string ORIGINAL_FILENAME_KEY = "OriginalFileName";

        public BlobService(BlobServiceClient blobServiceClient, IAuthService authService)
        {
            _blobServiceClient = blobServiceClient;
            _authService = authService;
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string containerName)
        {
            var userId = _authService.GetUserId() ?? "anonymous";

            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

            // Sanitize the filename to ensure it only contains ASCII characters
            var sanitizedFileName = SanitizeFileName(fileName);

            // Include userId in blob name for organization and isolation
            var blobName = $"{userId}/{sanitizedFileName}";

            var blobClient = containerClient.GetBlobClient(blobName);

            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(fileName, out string? contentType))
            {
                contentType = "application/octet-stream"; // Default if type not found
            }

            var blobHttpHeaders = new BlobHttpHeaders { ContentType = contentType };

            // Store metadata with ASCII-safe values
            var metadata = new Dictionary<string, string>
            {
                { USER_ID_METADATA_KEY, SanitizeForMetadata(userId) },
                { ORIGINAL_FILENAME_KEY, EncodeForMetadata(fileName) }, // Encode the original filename
                { "UploadedAt", DateTimeOffset.UtcNow.ToString("O") }
            };

            var options = new BlobUploadOptions
            {
                HttpHeaders = blobHttpHeaders,
                Metadata = metadata
            };

            await blobClient.UploadAsync(fileStream, options);

            return blobClient.Uri.ToString();
        }

        public async Task<(Stream Content, string Url)> DownloadFileAsync(string fileName, string containerName)
        {
            var userId = _authService.GetUserId() ?? "anonymous";

            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

            // Sanitize the filename for lookup
            var sanitizedFileName = SanitizeFileName(fileName);

            // Construct the full blob path with userId
            var blobName = $"{userId}/{sanitizedFileName}";
            var blobClient = containerClient.GetBlobClient(blobName);

            // Check if blob exists
            if (!await blobClient.ExistsAsync())
            {
                throw new FileNotFoundException($"File {fileName} not found for the current user.");
            }

            // Additional security: verify user ID in metadata
            var properties = await blobClient.GetPropertiesAsync();
            if (properties.Value.Metadata.TryGetValue(USER_ID_METADATA_KEY, out var storedUserId))
            {
                var sanitizedStoredUserId = SanitizeForMetadata(userId);
                if (storedUserId != sanitizedStoredUserId)
                {
                    throw new UnauthorizedAccessException($"User is not authorized to access this file.");
                }
            }

            var response = await blobClient.DownloadAsync();
            return (response.Value.Content, blobClient.Uri.ToString());
        }

        public async Task<List<string>> ListFilesAsync(string containerName, string[]? extensions = null)
        {
            var userId = _authService.GetUserId() ?? "anonymous";

            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

            List<string> fileNames = new List<string>();

            // Use prefix to filter blobs by userId
            var prefix = $"{userId}/";

            await foreach (BlobItem blobItem in containerClient.GetBlobsAsync(BlobTraits.Metadata, prefix: prefix))
            {
                // Try to get the original filename from metadata
                string displayFileName;

                if (blobItem.Metadata != null &&
                    blobItem.Metadata.TryGetValue(ORIGINAL_FILENAME_KEY, out var encodedOriginalName))
                {
                    // Decode the original filename
                    displayFileName = DecodeFromMetadata(encodedOriginalName);
                }
                else
                {
                    // Fall back to extracting from blob name
                    displayFileName = blobItem.Name.Substring(prefix.Length);
                }

                // Apply extension filter if provided
                if (extensions == null || extensions.Length == 0 ||
                    extensions.Any(ext => displayFileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
                {
                    // Verify user ID in metadata for security
                    if (blobItem.Metadata != null &&
                        blobItem.Metadata.TryGetValue(USER_ID_METADATA_KEY, out var storedUserId))
                    {
                        var sanitizedUserId = SanitizeForMetadata(userId);
                        if (storedUserId == sanitizedUserId)
                        {
                            fileNames.Add(displayFileName);
                        }
                    }
                    else if (blobItem.Metadata == null || !blobItem.Metadata.ContainsKey(USER_ID_METADATA_KEY))
                    {
                        // Include files without metadata (for backward compatibility)
                        fileNames.Add(displayFileName);
                    }
                }
            }

            return fileNames;
        }

        public string GenerateSasUri(string blobName, string containerName, int expiryMinutes = 60)
        {
            var userId = _authService.GetUserId() ?? "anonymous";

            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

            // Sanitize the blob name
            var sanitizedBlobName = SanitizeFileName(blobName);

            // Construct the full blob path with userId
            var fullBlobPath = $"{userId}/{sanitizedBlobName}";
            var blobClient = containerClient.GetBlobClient(fullBlobPath);

            // Verify the blob exists and belongs to the user
            if (!blobClient.Exists())
            {
                throw new FileNotFoundException($"File {blobName} not found for the current user.");
            }

            // Additional security: verify user ID in metadata
            var properties = blobClient.GetProperties();
            if (properties.Value.Metadata.TryGetValue(USER_ID_METADATA_KEY, out var storedUserId))
            {
                var sanitizedUserId = SanitizeForMetadata(userId);
                if (storedUserId != sanitizedUserId)
                {
                    throw new UnauthorizedAccessException($"User is not authorized to access this file.");
                }
            }

            if (!blobClient.CanGenerateSasUri)
            {
                throw new InvalidOperationException("SAS generation not supported for this blob client. Ensure the client is initialized with StorageSharedKeyCredential.");
            }

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = containerName,
                BlobName = fullBlobPath,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(expiryMinutes)
            };

            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            var sasUri = blobClient.GenerateSasUri(sasBuilder);
            return sasUri.ToString();
        }

        /// <summary>
        /// Sanitizes a filename to ensure it only contains ASCII characters safe for blob storage
        /// </summary>
        private string SanitizeFileName(string fileName)
        {
            // Get the file extension
            var extension = Path.GetExtension(fileName);
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);

            // Replace non-ASCII and problematic characters with safe alternatives
            var sanitized = Regex.Replace(nameWithoutExtension, @"[^\w\-\.]", "_");

            // Ensure the filename is not empty
            if (string.IsNullOrWhiteSpace(sanitized))
            {
                sanitized = $"file_{Guid.NewGuid():N}";
            }

            // Add a hash of the original filename to maintain uniqueness
            if (nameWithoutExtension != sanitized)
            {
                var hash = GetStableHash(fileName);
                sanitized = $"{sanitized}_{hash}";
            }

            return sanitized + extension;
        }

        /// <summary>
        /// Sanitizes a string for use in metadata (must be ASCII)
        /// </summary>
        private string SanitizeForMetadata(string value)
        {
            // Replace non-ASCII characters with underscores
            return Regex.Replace(value, @"[^\x00-\x7F]", "_");
        }

        /// <summary>
        /// Encodes a string for safe storage in metadata
        /// </summary>
        private string EncodeForMetadata(string value)
        {
            // Base64 encode to ensure ASCII safety
            var bytes = Encoding.UTF8.GetBytes(value);
            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// Decodes a string from metadata storage
        /// </summary>
        private string DecodeFromMetadata(string encodedValue)
        {
            try
            {
                var bytes = Convert.FromBase64String(encodedValue);
                return Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                // If decoding fails, return the original value
                return encodedValue;
            }
        }

        /// <summary>
        /// Generates a stable hash for a given string
        /// </summary>
        private string GetStableHash(string input)
        {
            using var md5 = System.Security.Cryptography.MD5.Create();
            var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
            return BitConverter.ToString(hash).Replace("-", "").Substring(0, 8).ToLower();
        }
    }
}