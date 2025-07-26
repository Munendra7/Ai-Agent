using DocumentFormat.OpenXml.Drawing.Diagrams;
using Microsoft.SemanticKernel;
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

        [KernelFunction("ExtractTemplateParameters"), Description("Extract all required placeholders (text and tables) from a selected template")]
        public async Task<HashSet<string>> ExtractTemplateParametersAsync(string templateFileName)
        {
            var templateStream = await blobService.DownloadFileAsync(templateFileName, BlobStorageConstants.TemplateContainerName);
            if (templateStream.Content == null) return new HashSet<string>();

            return documentsProcessFactory.ExtractPlaceholders(templateStream.Content);
        }

        [KernelFunction("GenerateDocument"), Description("Generate a document from a template with user input, it returns the URL of the file")]
        public async Task<string> GenerateDocumentAsync([Description("Will take template name and placeholders to replace template placeholders. Normal Text fields should be in the format {key:value} and Tabular Data should be in the format [{ColName1:'value1', ColName2:'value2'}, {ColName1:'value3', ColName2:'value4'}]. Both are part of my dynamicInputs variable")] string templateFileName, Dictionary<string, object> dynamicInputs)
        {
            var templateStream = await blobService.DownloadFileAsync(templateFileName, BlobStorageConstants.TemplateContainerName);
            if (templateStream.Content == null) return "Template not found.";

            var generatedDocStream = documentsProcessFactory.ReplacePlaceholdersInDocx(templateStream.Content, dynamicInputs);

            string newFileName = $"Generated_{Guid.NewGuid()}.docx";
            string documentUrl = await blobService.UploadFileAsync(generatedDocStream, newFileName, BlobStorageConstants.GeneratedDocsContainerName);

            return documentUrl.Replace(BlobStorageConstants.StorageImageName, "localhost");
        }
    }
}
