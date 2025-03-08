using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Planning.Handlebars;
using SemanticKernel.AIAgentBackend.CustomActionFilters;
using SemanticKernel.AIAgentBackend.Models.DTO;
using SemanticKernel.AIAgentBackend.plugins.NativePlugin;
using SemanticKernel.AIAgentBackend.Plugins.NativePlugin;
using SemanticKernel.AIAgentBackend.Repositories.Interface;

namespace SemanticKernel.AIAgentBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly Kernel _kernel;
        private readonly IConfiguration _configuration;
        private readonly IChatService _chatService;
        private readonly IEmbeddingService embeddingService;

        public ChatController([FromKeyedServices("LLMKernel")] Kernel kernel, IConfiguration configuration, IChatService chatService, IEmbeddingService embeddingService)
        {
            _kernel = kernel;
            _configuration = configuration;
            _chatService = chatService;
            this.embeddingService = embeddingService;
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
            try
            {

                var chatPlugin = new BasicChatPlugin(_kernel, _chatService, hardcodeduserId);
                var weatherPlugin = new WeatherPlugin(_kernel, _configuration);
                var googleSearchPlugin = new GoogleSearchPlugin(_kernel, _configuration);
                var ragPlugin = new RAGPlugin(_kernel, embeddingService);


                _kernel.ImportPluginFromObject(weatherPlugin, "WeatherPlugin");
                _kernel.ImportPluginFromObject(googleSearchPlugin, "GoogleSearchPlugin");
                _kernel.ImportPluginFromObject(chatPlugin, "BasicChatPlugin");
                _kernel.ImportPluginFromObject(ragPlugin, "RAGPlugin");

                #pragma warning disable SKEXP0060 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                var planner = new HandlebarsPlanner(new HandlebarsPlannerOptions() { AllowLoops = true });
                #pragma warning restore SKEXP0060 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

                // Suppress the diagnostic warning for CreatePlanAsync
                #pragma warning disable SKEXP0060
                var plan = await planner.CreatePlanAsync(_kernel, userQueryDTO.Query);
                #pragma warning restore SKEXP0060

                var serializedPlan = plan.ToString();

                // Suppress the diagnostic warning for InvokeAsync
                #pragma warning disable SKEXP0060
                var result = await plan.InvokeAsync(_kernel);
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
                    var chatResponse = await _kernel.InvokeAsync("BasicChatPlugin", "chat", new() { ["query"] = userQueryDTO.Query });

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