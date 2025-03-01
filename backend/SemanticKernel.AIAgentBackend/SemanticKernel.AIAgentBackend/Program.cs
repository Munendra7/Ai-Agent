using SemanticKernel.AIAgentBackend.Middlewares;
using SemanticKernel.AIAgentBackend.Plugins;
using SemanticKernel.AIAgentBackend.Repositories;
using SemanticKernel.AIAgentBackend.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IKernelService, KernelService>();
builder.Services.AddHttpClient();

var configuration = builder.Configuration;
string googleApiKey = configuration["GoogleSearch:ApiKey"]!;
string googleCseId = configuration["GoogleSearch:SearchEngineId"]!;

builder.Services.AddScoped<GoogleSearchPlugin>(sp =>
    new GoogleSearchPlugin(sp.GetRequiredService<HttpClient>(), googleApiKey, googleCseId));

builder.Services.AddScoped<ISearchService, SearchService>();

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
