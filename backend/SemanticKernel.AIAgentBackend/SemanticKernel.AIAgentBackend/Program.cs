using SemanticKernel.AIAgentBackend.Data;
using SemanticKernel.AIAgentBackend.Middlewares;
using Microsoft.EntityFrameworkCore;
using Qdrant.Client;
using SemanticKernel.AIAgentBackend.Factories.Interface;
using SemanticKernel.AIAgentBackend.Factories.Factory;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;
using SemanticKernel.AIAgentBackend.Repositories.Interface;
using SemanticKernel.AIAgentBackend.Repositories.Repository;
using Serilog; // Add this using directive

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.


var logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("Logs/AIAgent_log.txt", rollingInterval: RollingInterval.Day)
    .MinimumLevel.Information()
    .CreateLogger();

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(logger);

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("AppDBConnectionString"));
});

builder.Services.AddScoped<QdrantClient>(provider =>
{
    var qdrantUri = new Uri(builder.Configuration["Qdrant:Endpoint"] ?? "http://localhost:6334");
    return new QdrantClient(qdrantUri);
});


builder.Services.AddScoped<IKernelFactory, KernelFactory>();
builder.Services.AddScoped<IEmbeddingKernelFactory, EmbeddingKernelFactory>();
builder.Services.AddScoped<IDocumentsProcessFactory, DocumentsProcessFactory>();

builder.Services.AddKeyedScoped<Kernel>("LLMKernel", (sp, key) =>
{
    var factory = sp.GetRequiredService<IKernelFactory>();
    return factory.CreateKernel();
});

builder.Services.AddScoped(sp =>
{
    var factory = sp.GetRequiredService<IEmbeddingKernelFactory>();
    var Kernel = factory.CreateKernel();
    #pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    var _embeddingGenerator = Kernel.Services.GetRequiredService<ITextEmbeddingGenerationService>();
    #pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    return _embeddingGenerator;
});


builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IEmbeddingService, EmbeddingService>();

builder.Services.AddHttpClient();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseMiddleware<ExceptionHandlerMiddleware>();

app.UseAuthorization();

app.MapControllers();

app.Run();
