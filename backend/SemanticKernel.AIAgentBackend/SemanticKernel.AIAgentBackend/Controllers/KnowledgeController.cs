﻿using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using SemanticKernel.AIAgentBackend.CustomActionFilters;
using SemanticKernel.AIAgentBackend.Models.DTO;
using SemanticKernel.AIAgentBackend.Repositories.Interface;
using SemanticKernel.AIAgentBackend.Repositories.Repository;
using SemanticKernel.AIAgentBackend.Constants;
using Azure.Core;

namespace SemanticKernel.AIAgentBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class KnowledgeController : ControllerBase
    {
        private readonly IEmbeddingService embeddingService;
        private readonly IBlobService blobService;

        public KnowledgeController(IEmbeddingService embeddingService, IBlobService blobService)
        {
            this.embeddingService = embeddingService;
            this.blobService = blobService;
        }

        [HttpPost]
        [ValidateModel]
        [Route("Upload")]
        public async Task<IActionResult> UploadKnowledgeAsync([FromForm] FileUploadDTO fileDTO)
        {
            ValidateFileUpload(fileDTO);

            if (ModelState.IsValid)
            {
                var filePath = await blobService.UploadFileAsync(fileDTO.File.OpenReadStream(), fileDTO.File.FileName, BlobStorageConstants.KnowledgeContainerName);

                var embeddingResponse = KnowledgeContants.EmbeddingSupportedFiles.Contains(Path.GetExtension(fileDTO.File.FileName))?await embeddingService.ProcessFileAsync(fileDTO, filePath):"File Stored Successfully";

                return Ok(new FileUploadResponseDTO()
                {
                    FileProcessResponse = embeddingResponse,
                    FilePath = filePath
                });
            }

            return BadRequest(ModelState);
        }

        [HttpPost]
        [ValidateModel]
        [Route("Upload/AddTemplate")]
        public async Task<IActionResult> UploadTemplateAsync(TemplateUploadDTO templateDTO)
        {
            ValidateTemplateUpload(templateDTO);

            if (ModelState.IsValid)
            {
                var filePath = await blobService.UploadFileAsync(templateDTO.File.OpenReadStream(), templateDTO.File.FileName, BlobStorageConstants.TemplateContainerName);

                return Ok(new FileUploadResponseDTO()
                {
                    FileProcessResponse = "File uploaded successfully",
                    FilePath = filePath
                });
            }

            return BadRequest(ModelState);
        }

        private void ValidateFileUpload(FileUploadDTO request)
        {
            if (!KnowledgeContants.AllowedDocumentExtensions.Contains(Path.GetExtension(request.File.FileName)))
            {
                ModelState.AddModelError("file", "Unsuported File");
            }

            if (request.File.Length > 10485760)
            {
                ModelState.AddModelError("file", "Please add file less than 10 MB");
            }
        }

        private void ValidateTemplateUpload(TemplateUploadDTO request)
        {
            if (!KnowledgeContants.AllowedTemplateExtensions.Contains(Path.GetExtension(request.File.FileName)))
            {
                ModelState.AddModelError("file", "Unsuported File");
            }

            if (request.File.Length > 10485760)
            {
                ModelState.AddModelError("file", "Please add file less than 10 MB");
            }
        }
    }
}
