using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using SemanticKernel.AIAgentBackend.Models.DTO;
using SemanticKernel.AIAgentBackend.Repositories;
using SemanticKernel.AIAgentBackend.Services;
using System.Threading.Tasks;

namespace SemanticKernel.AIAgentBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly ISearchService _searchService;
        private readonly IKernelService _kernel;

        public ChatController(ISearchService searchService, IKernelService kernel)
        {
            _searchService = searchService;
            _kernel = kernel;
        }

        [HttpPost]
        public async Task<IActionResult> ChatAsync([FromBody] UserQueryDTO userQueryDTO)
        {
            if (string.IsNullOrWhiteSpace(userQueryDTO.Query))
            {
                return BadRequest("Question cannot be empty.");
            }

            var kernel = _kernel.GetKernel(userQueryDTO.Model);

            var response = await _searchService.GetSemanticSearchResponseAsync(userQueryDTO.Query, kernel);
            return Ok(response);
        }
    }
}