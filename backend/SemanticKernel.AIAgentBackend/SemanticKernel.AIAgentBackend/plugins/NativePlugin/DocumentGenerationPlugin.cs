using DocumentFormat.OpenXml.Drawing.Diagrams;
using DocxProcessorLibrary.TemplateBasedDocGenerator;
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
        private readonly ITemplateBasedDocGenerator templatebasedDocGenerator;
        private readonly IConfiguration configuration;

        public DocumentGenerationPlugin(IBlobService blobService, ITemplateBasedDocGenerator templateBasedDocGenerator, IConfiguration configuration)
        {
            this.blobService = blobService;
            this.templatebasedDocGenerator = templateBasedDocGenerator;
            this.configuration = configuration;
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

            using var memoryStream = new MemoryStream();
            await templateStream.Content.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            var payload = templatebasedDocGenerator.ExtractRequiredPayload(memoryStream);
            return JsonConvert.SerializeObject(payload, Formatting.Indented);
        }

        [KernelFunction("GenerateDocument"), Description("Generate a document from a template with user input payload, it returns the URL of the file. Make sure proper payload is given.")]
        public async Task<string> GenerateDocumentAsync([Description("Will take template name and placeholders to replace template placeholders.")] string templateFileName, [Description("send the template json payload in format returned by KernelFunction ExtractTemplatePayload for this template")] string templatePayload)
        {
            string docxtopdfserviceURl = configuration["DocToPDFService:EndPoint"]!;

            var templateStream = await blobService.DownloadFileAsync(templateFileName, BlobStorageConstants.TemplateContainerName);
            if (templateStream.Content == null) return "Template not found.";

            using var memoryStream = new MemoryStream();
            await templateStream.Content.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            var generatedDocStream = templatebasedDocGenerator.PopulateContentControlsFromJson(memoryStream, templatePayload);

            //string newFileName = $"Generated_{Guid.NewGuid()}.docx";
            //string documentUrl = await blobService.UploadFileAsync(generatedDocStream, newFileName, BlobStorageConstants.GeneratedDocsContainerName);

            //return documentUrl.Replace(BlobStorageConstants.StorageImageName, "localhost");

            generatedDocStream.Position = 0;

            // Prepare content for docx-pdf-service (multipart form-data)
            using var httpClient = new HttpClient
            {
                BaseAddress = new Uri(docxtopdfserviceURl)
            };

            using var content = new MultipartFormDataContent();
            content.Add(new StreamContent(generatedDocStream)
            {
                Headers =
                {
                    ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("form-data")
                    {
                        Name = "\"file\"",
                        FileName = "\"document.docx\""
                    },
                    ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.wordprocessingml.document")
                }
            });

            // Send to Node.js service
            var response = await httpClient.PostAsync("/convert", content);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return $"PDF conversion failed: {error}";
            }

            // Upload PDF to blob storage
            var pdfStream = await response.Content.ReadAsStreamAsync();
            string pdfFileName = $"Generated_{Guid.NewGuid()}.pdf";
            string pdfUrl = await blobService.UploadFileAsync(pdfStream, pdfFileName, BlobStorageConstants.GeneratedDocsContainerName);

            return blobService.GenerateSasUri(pdfFileName, BlobStorageConstants.GeneratedDocsContainerName, 1440).Replace(BlobStorageConstants.StorageImageName, "localhost");

            //return pdfUrl.Replace(BlobStorageConstants.StorageImageName, "localhost");
        }
    }
}
