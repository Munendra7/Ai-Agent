using DocumentFormat.OpenXml.Drawing.Diagrams;
using Microsoft.SemanticKernel;
using Newtonsoft.Json;
using SemanticKernel.AIAgentBackend.Constants;
using SemanticKernel.AIAgentBackend.Factories.Interface;
using SemanticKernel.AIAgentBackend.Repositories.Interface;
using System.ComponentModel;

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

        [KernelFunction("ExtractTemplatePayload"), Description("Extract required json payload from template which will help for document generation")]
        public async Task<string> ExtractTemplatePayloadAsync(string templateFileName)
        {
            var templateStream = await blobService.DownloadFileAsync(templateFileName, BlobStorageConstants.TemplateContainerName);
            if (templateStream.Content == null) return "";

            var payload = documentsProcessFactory.ExtractRequiredPayload(templateStream.Content);
            return JsonConvert.SerializeObject(payload, Formatting.Indented);
        }

        [KernelFunction("GenerateDocument"), Description("Generate a document from a template with user input payload, it returns the URL of the file")]
        public async Task<string> GenerateDocumentAsync([Description("Will take template name and placeholders to replace template placeholders.")] string templateFileName, [Description("send the template json payload in format returned by KernelFunction ExtractTemplatePayload for this template")] string templatePayload)
        {
            var templateStream = await blobService.DownloadFileAsync(templateFileName, BlobStorageConstants.TemplateContainerName);
            if (templateStream.Content == null) return "Template not found.";

            var generatedDocStream = documentsProcessFactory.PopulateContentControlsFromJson(templateStream.Content, templatePayload);

            string newFileName = $"Generated_{Guid.NewGuid()}.docx";
            string documentUrl = await blobService.UploadFileAsync(generatedDocStream, newFileName, BlobStorageConstants.GeneratedDocsContainerName);

            return documentUrl.Replace(BlobStorageConstants.StorageImageName, "localhost");
        }
    }
}
