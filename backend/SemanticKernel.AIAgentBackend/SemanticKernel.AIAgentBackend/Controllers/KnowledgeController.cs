using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using SemanticKernel.AIAgentBackend.CustomActionFilters;
using SemanticKernel.AIAgentBackend.Models.DTO;
using SemanticKernel.AIAgentBackend.Repositories.Interface;

namespace SemanticKernel.AIAgentBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class KnowledgeController : ControllerBase
    {
        private readonly IEmbeddingService embeddingService;

        public KnowledgeController(IEmbeddingService embeddingService)
        {
            this.embeddingService = embeddingService;
        }

        [HttpPost]
        [ValidateModel]
        [Route("Upload")]
        public async Task<IActionResult> UploadKnowledgeAsync([FromForm] FileUploadDTO fileDTO)
        {
            ValidateFileUpload(fileDTO);

            if(ModelState.IsValid)
            {
                var response = await embeddingService.ProcessFileAsync(fileDTO);
                
                return Ok(response);
            }

            return BadRequest(ModelState);
        }


        private void ValidateFileUpload(FileUploadDTO request)
        {
            var allowedExtentions = new string[] { ".pdf", ".docx" , ".txt", ".xlsx", ".csv" };

            if (!allowedExtentions.Contains(Path.GetExtension(request.File.FileName)))
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
