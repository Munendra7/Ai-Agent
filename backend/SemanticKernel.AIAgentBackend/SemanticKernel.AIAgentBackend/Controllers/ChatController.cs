using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using Microsoft.OpenApi.Services;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Planning.Handlebars;
using SemanticKernel.AIAgentBackend.CustomActionFilters;
using SemanticKernel.AIAgentBackend.Models.DTO;
using SemanticKernel.AIAgentBackend.plugins.NativePlugin;
using SemanticKernel.AIAgentBackend.Plugins.NativePlugin;
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
        private readonly IChatService _chatService;

        public ChatController(IKernelService kernel, IConfiguration configuration, IChatService chatService)
        {
            _kernel = kernel;
            _configuration = configuration;
            _chatService = chatService;
        }

        [HttpPost]
        [ValidateModel]
        public async Task<IActionResult> ChatAsync([FromBody] UserQueryDTO userQueryDTO)
        {
            if (string.IsNullOrWhiteSpace(userQueryDTO.Query))
            {
                return BadRequest("Question cannot be empty.");
            }


            var hardcodeduserId = "1234";

            await _chatService.AddMessageAsync(hardcodeduserId, userQueryDTO.Query, "User");

            var kernel = _kernel.GetKernel(userQueryDTO.Model);
            try
            {

                var chatPlugin = new BasicChatPlugin(_kernel, userQueryDTO.Model, _chatService, hardcodeduserId);
                var weatherPlugin = new WeatherPlugin(_configuration, _kernel, userQueryDTO.Model);
                var googleSearchPlugin = new GoogleSearchPlugin(_configuration, _kernel, userQueryDTO.Model);


                kernel.ImportPluginFromObject(weatherPlugin, "WeatherPlugin");
                kernel.ImportPluginFromObject(googleSearchPlugin, "GoogleSearchPlugin");
                kernel.ImportPluginFromObject(chatPlugin, "BasicChatPlugin");

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

                await _chatService.AddMessageAsync(hardcodeduserId, chatResponse, "Bot");

                return Ok(response);
            }

            catch (Exception)
            {
                try
                {
                    var chatResponse = await kernel.InvokeAsync("BasicChatPlugin", "chat", new() { ["query"] = userQueryDTO.Query });

                    var response = new ChatResponseDTO()
                    {
                        Response = chatResponse.ToString(),
                        SerializedPlan = "used basic chat plugin"
                    };

                    await _chatService.AddMessageAsync(hardcodeduserId, chatResponse.ToString(), "Bot");
                    return Ok(response);
                }

                catch (Exception)
                {
                    return StatusCode(500, new
                    {
                        error = "Something went wrong on our end. Please try again later.",
                    });
                }
            }
        }
    }
}