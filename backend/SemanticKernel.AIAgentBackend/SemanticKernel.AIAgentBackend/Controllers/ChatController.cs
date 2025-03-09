using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Planning.Handlebars;
using SemanticKernel.AIAgentBackend.CustomActionFilters;
using SemanticKernel.AIAgentBackend.Models.Domain;
using SemanticKernel.AIAgentBackend.Models.DTO;
using SemanticKernel.AIAgentBackend.plugins.NativePlugin;
using SemanticKernel.AIAgentBackend.Plugins.NativePlugin;
using SemanticKernel.AIAgentBackend.Repositories.Interface;
using SemanticKernel.AIAgentBackend.Repositories.Repository;

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

            var sessionId = new Guid("4963c532-26f9-4bea-92ae-67c0c3c05700");

            var conversationHistory = await _chatService.GetMessagesAsync(sessionId, 5);
            var historyContext = string.Join("\n", conversationHistory.Select(m => $"{m.Sender}: {m.Message}"));
            try
            {
                var chatPlugin = new BasicChatPlugin(_kernel, _chatService, sessionId);
                var weatherPlugin = new WeatherPlugin(_kernel, _configuration);
                var googleSearchPlugin = new GoogleSearchPlugin(_kernel, _configuration);
                var ragPlugin = new RAGPlugin(_kernel, embeddingService);

                _kernel.ImportPluginFromObject(weatherPlugin, "WeatherPlugin");
                _kernel.ImportPluginFromObject(googleSearchPlugin, "GoogleSearchPlugin");
                _kernel.ImportPluginFromObject(chatPlugin, "BasicChatPlugin");
                _kernel.ImportPluginFromObject(ragPlugin, "RAGPlugin");

                #pragma warning disable SKEXP0060 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                var planner = new HandlebarsPlanner(new HandlebarsPlannerOptions()
                {
                    ExecutionSettings = new OpenAIPromptExecutionSettings()
                    {
                        Temperature = 0.0,
                        TopP = 0.1,
                        ChatSystemPrompt = "You are an AI assistant that uses built in plugins to answer user query. if you don't have answer just do go outside the context you have."
                    },
                    AllowLoops = true,
                    GetAdditionalPromptContext = () => Task.FromResult($"Conversation History:\n{historyContext}")
                    
                    //GetAdditionalPromptContext = () => Task.FromResult("If the goal cannot be fully achieved with the provided helpers, call the `\\{BasicChatPlugin.chat}` helper."),
                });
                #pragma warning restore SKEXP0060 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

                // Suppress the diagnostic warning for CreatePlanAsync
                #pragma warning disable SKEXP0060
                var plan = await planner.CreatePlanAsync(_kernel, userQueryDTO.Query);
                #pragma warning restore SKEXP0060

                var serializedPlan = plan.ToString();

                await _chatService.AddKernelPlannarLogsAsync(
                    new KernelPlannarLogs()
                    {
                        PlannarText = serializedPlan,
                        Timestamp = DateTime.Now
                    }
                );

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

                await _chatService.AddMessagesAsync(
                    new List<ChatHistory>()
                    {
                        new ChatHistory()
                        {
                            SessionId = sessionId,
                            Message = userQueryDTO.Query,
                            Sender = "User",
                            Timestamp = DateTime.Now
                        },
                        new ChatHistory()
                        {
                            SessionId = sessionId,
                            Message = chatResponse,
                            Sender = "Bot",
                            Timestamp = DateTime.Now
                        }
                    }
                );

                return Ok(response);
            }

            catch (Exception ex)
            {
                try
                {
                    await _chatService.AddKernelPlannarLogsAsync(
                    new KernelPlannarLogs()
                        {
                            PlannarText = "used basic chat plugin",
                            Exception = ex.Message,
                            Timestamp = DateTime.Now
                        }
                    );

                    var chatResponse = await _kernel.InvokeAsync("BasicChatPlugin", "chat", new() { ["query"] = userQueryDTO.Query });

                    var response = new ChatResponseDTO()
                    {
                        Response = chatResponse.ToString(),
                        SerializedPlan = "used basic chat plugin"
                    };

                    await _chatService.AddMessagesAsync(
                        new List<ChatHistory>()
                        {
                            new ChatHistory()
                            {
                                SessionId = sessionId,
                                Message = userQueryDTO.Query,
                                Sender = "User",
                                Timestamp = DateTime.Now
                            },
                            new ChatHistory()
                            {
                                SessionId = sessionId,
                                Message = chatResponse.ToString(),
                                Sender = "Bot",
                                Timestamp = DateTime.Now
                            }
                        }
                    );

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