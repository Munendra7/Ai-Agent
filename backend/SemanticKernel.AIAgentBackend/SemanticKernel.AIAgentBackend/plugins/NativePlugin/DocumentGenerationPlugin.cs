using Azure.Storage.Blobs;
using DocumentFormat.OpenXml.Drawing.Diagrams;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.SemanticKernel;
using SemanticKernel.AIAgentBackend.Constants;
using SemanticKernel.AIAgentBackend.Factories.Interface;
using SemanticKernel.AIAgentBackend.Repositories.Interface;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace SemanticKernel.AIAgentBackend.plugins.NativePlugin
{
    public class DocumentGenerationPlugin
    {
        private readonly IBlobService blobService;
        private readonly IDocumentsProcessFactory documentsProcessFactory;

        public DocumentGenerationPlugin(IBlobService blobService, IDocumentsProcessFactory documentsProcessFactory)
        {
            this.blobService = blobService;
            this.documentsProcessFactory = documentsProcessFactory;
        }

        [KernelFunction("ListTemplates"), Description("List available document templates")]
        public async Task<List<string>> ListTemplatesAsync()
        {
            var templates = await blobService.ListFilesAsync(BlobStorageConstants.TemplateContainerName);

            return templates;
        }

        [KernelFunction("ExtractTemplateParameters"), Description("Extract required parameters and tables from a selected template")]
        public async Task<Dictionary<string, string>> ExtractTemplateParametersAsync(string templateFileName)
        {
            var templateStream = await blobService.DownloadFileAsync(templateFileName, BlobStorageConstants.TemplateContainerName);
            if (templateStream.Content == null) return new Dictionary<string, string>();

            return documentsProcessFactory.ExtractPlaceholders(templateStream.Content);
        }

        [KernelFunction("GenerateDocument"), Description("Generate a document from a template with user input, it returns the URL of File")]
        public async Task<string> GenerateDocumentAsync([Description("TableInputs is Options, where the parameters repalces the place holders in the document")] string templateFileName, Dictionary<string, string>? userInputs, Dictionary<string, List<List<string>>>? tableInputs)
        {
            // Download template
            var templateStream = await blobService.DownloadFileAsync(templateFileName, BlobStorageConstants.TemplateContainerName);
            if (templateStream.Content == null) return "Template not found.";

            // Replace placeholders in the document
            var generatedDocStream = documentsProcessFactory.ReplacePlaceholdersInDocx(templateStream.Content, userInputs, tableInputs);

            // Upload new document to Blob Storage
            string newFileName = $"Generated_{Guid.NewGuid()}.docx";
            string documentUrl = await blobService.UploadFileAsync(generatedDocStream, newFileName, BlobStorageConstants.GeneratedDocsContainerName);

            return documentUrl.Replace(BlobStorageConstants.StorageImageName, "localhost");
        }
    }
}
