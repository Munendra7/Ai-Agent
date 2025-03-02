using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Services;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Planning.Handlebars;
using SemanticKernel.AIAgentBackend.CustomActionFilters;
using SemanticKernel.AIAgentBackend.Models.DTO;
using SemanticKernel.AIAgentBackend.plugins.NativePlugin;
using SemanticKernel.AIAgentBackend.Repositories;
using System.Threading.Tasks;

namespace SemanticKernel.AIAgentBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly IKernelService _kernel;
        private readonly IConfiguration _configuration;

        public ChatController(IKernelService kernel, IConfiguration configuration)
        {
            _kernel = kernel;
            _configuration = configuration;
        }

        [HttpPost]
        [ValidateModel]
        public async Task<IActionResult> ChatAsync([FromBody] UserQueryDTO userQueryDTO)
        {
            if (string.IsNullOrWhiteSpace(userQueryDTO.Query))
            {
                return BadRequest("Question cannot be empty.");
            }

            var kernel = _kernel.GetKernel(userQueryDTO.Model);

            var weatherPlugin = new WeatherPlugin(_configuration, _kernel);
            var googleSearchPlugin = new GoogleSearchPlugin(_configuration, _kernel);

            kernel.ImportPluginFromObject(weatherPlugin, "WeatherPlugin");
            kernel.ImportPluginFromObject(googleSearchPlugin, "GoogleSearchPlugin");

            #pragma warning disable SKEXP0060 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            var planner = new HandlebarsPlanner(new HandlebarsPlannerOptions() { AllowLoops = true });
            #pragma warning restore SKEXP0060 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

            // Suppress the diagnostic warning for CreatePlanAsync
            #pragma warning disable SKEXP0060
            var plan = await planner.CreatePlanAsync(kernel, userQueryDTO.Query);
            #pragma warning restore SKEXP0060

            var serializedPlan = plan.ToString();

            // Suppress the diagnostic warning for InvokeAsync
            #pragma warning disable SKEXP0060
            var result = await plan.InvokeAsync(kernel);
            #pragma warning restore SKEXP0060

            var chatResponse = result.ToString();

            var stringSerializedPlan = serializedPlan.ToString();

            var response = new ChatResponseDTO()
            {
                Response = chatResponse,
                SerializedPlan = stringSerializedPlan
            };

            return Ok(response);
        }
    }
}