using DocxProcessorLibrary.TemplateBasedDocGenerator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.ChatCompletion;
using SemanticKernel.AIAgentBackend.Factories.Interface;
using SemanticKernel.AIAgentBackend.Models.DTO;
using SemanticKernel.AIAgentBackend.plugins.NativePlugin;
using SemanticKernel.AIAgentBackend.Repositories.Interface;
using ChatHistory = SemanticKernel.AIAgentBackend.Models.Domain.ChatHistory;

namespace SemanticKernel.AIAgentBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MultiAgentController : ControllerBase
    {
        private readonly Kernel _kernel;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IChatHistoryService _chatService;
        private readonly IEmbeddingService _embeddingService;
        private readonly IBlobService _blobService;
        private readonly ILogger _logger;
        private readonly IAgentFactory _agentFactory;
        private readonly ITemplateBasedDocGenerator _templateBasedDocGenerator;

        public MultiAgentController([FromKeyedServices("LLMKernel")] Kernel kernel, HttpClient httpClient, IConfiguration configuration, IChatHistoryService chatService, IEmbeddingService embeddingService, IBlobService blobService, IAgentFactory agentFactory, ILogger<ChatController> logger, ITemplateBasedDocGenerator templateBasedDocGenerator)
        {
            _kernel = kernel;
            _httpClient = httpClient;
            _configuration = configuration;
            _chatService = chatService;
            _embeddingService = embeddingService;
            _blobService = blobService;
            _agentFactory = agentFactory;
            _logger = logger;
            _templateBasedDocGenerator = templateBasedDocGenerator;
        }

        [HttpPost]
        [Route("Chat")]
        public async Task<IActionResult> ChatAsync([FromBody] UserQueryDTO userQueryDTO)
        {
            if (string.IsNullOrWhiteSpace(userQueryDTO.Query))
            {
                return BadRequest("Question cannot be empty.");
            }

            try
            {
                #pragma warning disable SKEXP0110
                var weatherPlugin = new WeatherPlugin(_kernel, _configuration);
                var googleSearchPlugin = new GoogleSearchPlugin(_kernel, _configuration);
                var ragPlugin = new RAGPlugin(_kernel, _embeddingService, _blobService, _configuration);
                var emailWriterPlugin = new EmailWriterPlugin(_httpClient, _configuration);
                var documentGenerationPlugin = new DocumentGenerationPlugin(_blobService, _templateBasedDocGenerator, _configuration);

                Microsoft.SemanticKernel.ChatCompletion.ChatHistory chatHistory = new Microsoft.SemanticKernel.ChatCompletion.ChatHistory();

                var userChatHistory = await _chatService.GetMessagesAsync(userQueryDTO.SessionId, new Guid(), 10);

                foreach (var chat in userChatHistory)
                {
                    if (chat.Sender == "User")
                        chatHistory.AddUserMessage(chat.Message!);
                    else
                        chatHistory.AddAssistantMessage(chat.Message!);
                }

                chatHistory.AddUserMessage(userQueryDTO.Query);

                AgentThread thread = new ChatHistoryAgentThread(chatHistory);

                // Define agent names
                const string RagAgentName = "RAGAgent";
                const string APICallerAgentName = "APICallerAgent";
                const string EmailWriterAgentName = "EmailWriterAgent";
                const string DocumentGenerationAgentName = "DocumentGenerationAgent";
                const string CoordinatorAgentName = "CoordinatorAgent";

                var ragAgent = _agentFactory.CreateAgent(_kernel, RagAgentName,
                    "Use Retrieval-Augmented Generation (RAG) to fetch and return information from relevant documents only. If no relevant information is found, reply 'No relevant information found'.",
                    new List<object> { ragPlugin });

                var apiCallerAgent = _agentFactory.CreateAgent(_kernel, APICallerAgentName,
                    "Use GoogleSearchPlugin for web search and WeatherPlugin for weather lookup based on user's query.",
                    new List<object> { googleSearchPlugin, weatherPlugin });

                var emailWriterAgent = _agentFactory.CreateAgent(_kernel, EmailWriterAgentName,
                    "Send an email based on the user’s query. The user can specify an email's purpose, recipient, and content.",
                    new List<object> { emailWriterPlugin });

                var documentGenerationAgent = _agentFactory.CreateAgent(_kernel, DocumentGenerationAgentName,
                    "Based on the user's query, create a document. List all available templates, outline all required parameters needed to generate the document, and then create the document accordingly.",
                    new List<object> { documentGenerationPlugin });

                var coordinatorAgent = _agentFactory.CreateAgent(_kernel, CoordinatorAgentName,
                    $"""
                    You are the CoordinatorAgent. You Only Decide which agent should be selected to acheieve task.
                    Select which agent should handle the request:
                    - Choose {RagAgentName} if query can be answered from agent documents.
                    - Choose {APICallerAgentName} if web/weather information is needed.
                    - Choose {EmailWriterAgentName} if the user requests for send email.
                    - Choose {DocumentGenerationAgentName} if the user requests document creation.
                    """);


                var selectionFunction = AgentGroupChat.CreatePromptFunctionForStrategy(
                    $$$"""
                    Examine RESPONSE and choose the next participant.
                    State only the name of the chosen participant without explanation.
                    Never choose the participant named in the RESPONSE unless necessary..

                    Choose only from these participants:
                      - {{{CoordinatorAgentName}}}
                      - {{{RagAgentName}}}
                      - {{{APICallerAgentName}}}
                      - {{{EmailWriterAgentName}}}
                      - {{{DocumentGenerationAgentName}}}

                    Always follow these rules when choosing the next participant:
                    - Determine the nature of the user's request and route it to the appropriate agent.
                    - If the user is reponsing to an agent, select that same agent.
                    - If unclear, select {{{CoordinatorAgentName}}}

                    RESPONSE:
                    {{$lastmessage}}
                    """,
                    safeParameterNames: "lastmessage"
                );

                var selectionStrategy = new KernelFunctionSelectionStrategy(selectionFunction, _kernel)
                {
                    InitialAgent = coordinatorAgent,
                    HistoryReducer = new ChatHistoryTruncationReducer(3),
                    HistoryVariableName = "lastmessage",
                    ResultParser = result => result.GetValue<string>()??""
                };

                var terminationFunction = AgentGroupChat.CreatePromptFunctionForStrategy(
                    $$$"""
                        Examine the RESPONSE and determine if the conversation with agents should continue.

                        Respond with Yes (only the word Yes) if:
                        - The agent is clear about the whole task is completed and not processing in further task
                        - The agent says the task is complete (e.g., "document created", "email sent", "search completed", "response ready", "here is your download link").
                        - The agent provides a download link, or shares a final result.
                        - The agent says "you can now", "download here", "access it here", or similar.
                        - The agent asks the user for input ("please provide", "waiting for input", "upload needed", "more details required").

                        Respond with No (only the word No) if:
                        - The agent is actively performing an action like searching, retrieving, writing, or generating content.
                        - The agent is processing a request without needing any new information from the user.
                        - The agent is saying it will do some task.

                        Only respond with a single word: Yes or No.

                        RESPONSE:
                        {{$lastmessage}}
                    """,
                    safeParameterNames: "lastmessage"
                );

                var terminationStrategy = new KernelFunctionTerminationStrategy(terminationFunction, _kernel)
                {
                    Agents = new[] { ragAgent, apiCallerAgent, emailWriterAgent, documentGenerationAgent },
                    HistoryReducer = new ChatHistoryTruncationReducer(5),
                    ResultParser = res => res.GetValue<string>()?.Equals("Yes", StringComparison.OrdinalIgnoreCase) ?? false,
                    HistoryVariableName = "lastmessage",
                    MaximumIterations = 5
                };

                var groupChat = new AgentGroupChat(ragAgent, apiCallerAgent, emailWriterAgent, documentGenerationAgent, coordinatorAgent)
                {
                    ExecutionSettings = new()
                    {
                        SelectionStrategy = selectionStrategy,
                        TerminationStrategy = terminationStrategy,
                    }
                };

                foreach (var msg in userChatHistory)
                {
                    var role = msg.Sender == "User" ? AuthorRole.User : AuthorRole.Assistant;
                    groupChat.AddChatMessage(new ChatMessageContent(role, msg.Message!));
                }

                groupChat.AddChatMessage(new ChatMessageContent(AuthorRole.User, userQueryDTO.Query));

                List<string> finalResponses = new();
                await foreach (var activity in groupChat.InvokeAsync())
                {
                    var content = activity.Content;
                    if (content != null)
                    {
                        finalResponses.Add(content);
                    }
                }

                await _chatService.AddMessagesAsync(new List<ChatHistory>
                {
                    new ChatHistory { SessionId = userQueryDTO.SessionId, Message = userQueryDTO.Query, Sender = "User", Timestamp = DateTime.UtcNow },
                    new ChatHistory { SessionId = userQueryDTO.SessionId, Message = finalResponses.Last(), Sender = "Assistant", Timestamp = DateTime.UtcNow }
                });

                return Ok(new { response = finalResponses.Last() });

#pragma warning restore SKEXP0110
            }

            catch (Exception ex)
            {
                var errorId = Guid.NewGuid();
                _logger.LogError(ex, $"{errorId} : ${ex.Message}");
                try
                {
                    var chatResponse = await _kernel.InvokeAsync("BasicChatPlugin", "chat", new() { ["query"] = userQueryDTO.Query });

                    var response = new ChatResponseDTO()
                    {
                        Response = chatResponse.ToString()
                    };

                    await _chatService.AddMessagesAsync(
                        new List<ChatHistory>()
                        {
                            new ChatHistory()
                            {
                                SessionId = userQueryDTO.SessionId,
                                Message = userQueryDTO.Query,
                                Sender = "User",
                                Timestamp = DateTime.Now
                            },
                            new ChatHistory()
                            {
                                SessionId = userQueryDTO.SessionId,
                                Message = chatResponse.ToString(),
                                Sender = "Assistant",
                                Timestamp = DateTime.Now
                            }
                        }
                    );

                    return Ok(response);
                }
                catch (Exception exe)
                {
                    _logger.LogError(ex, $"{errorId} : ${exe.Message}");
                    return StatusCode(500, new { error = "Something went wrong on our end. Please try again later." });
                }
            }
        }
    }
}