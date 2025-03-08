using SemanticKernel.AIAgentBackend.Data;
using SemanticKernel.AIAgentBackend.Middlewares;
using SemanticKernel.AIAgentBackend.Repositories;
using Microsoft.EntityFrameworkCore;
using Qdrant.Client; // Add this using directive

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

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
    var qdrantUri = new Uri(builder.Configuration["Qdrant:Endpoint"] ?? "http://localhost:6333"); // Ensure correct Qdrant URL
    return new QdrantClient(qdrantUri);
});


builder.Services.AddScoped<IKernelService, KernelService>();
builder.Services.AddScoped<IKernelEmbeddingService, KernelEmbeddingService>();

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
