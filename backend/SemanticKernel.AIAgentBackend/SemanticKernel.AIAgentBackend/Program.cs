using Azure.Storage.Blobs;
using DocxProcessorLibrary.TemplateBasedDocGenerator;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;
using Qdrant.Client;
using SemanticKernel.AIAgentBackend.Data;
using SemanticKernel.AIAgentBackend.Extentions.cs;
using SemanticKernel.AIAgentBackend.Factories.Interface;
using SemanticKernel.AIAgentBackend.Middlewares;
using SemanticKernel.AIAgentBackend.Models.Configuration;
using SemanticKernel.AIAgentBackend.Models.Domain;
using SemanticKernel.AIAgentBackend.Services.Interface;
using SemanticKernel.AIAgentBackend.Services.Service;
using Serilog;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;

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

//builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
//    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

// Configuration
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<OAuthSettings>(builder.Configuration.GetSection("OAuth"));

// Services
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IOAuthService, OAuthService>();
builder.Services.AddHttpClient<IOAuthService, OAuthService>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();


// Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>();
var key = Encoding.ASCII.GetBytes(jwtSettings!.Secret);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = true;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtSettings.Audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// Authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"));
    options.AddPolicy("RequireUserRole", policy => policy.RequireRole("User"));
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp",
        policy =>
        {
            policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
});

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("AppDBConnectionString"));
});

builder.Services.AddScoped<QdrantClient>(provider =>
{
    var qdrantUri = new Uri(builder.Configuration["Qdrant:Endpoint"] ?? "http://localhost:6334");
    return new QdrantClient(qdrantUri);
});

builder.Services.AddScoped<BlobServiceClient>(provider =>
{
    var connectionString = builder.Configuration.GetConnectionString("AzureBlobStorage");
    return new BlobServiceClient(connectionString);
});


builder.Services.AddScoped<ITemplateBasedDocGenerator, TemplateBasedDocGenerator>();

builder.Services.AddSemanticKernelServices(builder.Configuration);

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

builder.Services.AddHttpClient();

builder.Services.AddHttpContextAccessor();

// 1 GB = 1073741824 bytes -- Only for Testing (Not recommeneded)
const long Gigabyte = 1073741824L*3;

// Allow large multipart form data
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = Gigabyte;
});

// Optional: increase Kestrel request body limit
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = Gigabyte;
});

// ========== RATE LIMITING CONFIGURATION ==========
builder.Services.AddRateLimiter(options =>
{
    // Global rejection configuration
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        // Get user from JWT token or use anonymous identifier
        var userId = httpContext.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
        var endpoint = httpContext.Request.Path.Value ?? "unknown";

        // Partition by both user + endpoint
        var key = $"{userId}:{endpoint}";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: key,
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 50, // Default limit for all users
                Window = TimeSpan.FromMinutes(1)
            });
    });

    // Custom rejection response
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter = retryAfter.TotalSeconds.ToString();
        }

        await context.HttpContext.Response.WriteAsync(
            "Too many requests. Please try again later.", cancellationToken);
    };

    // Policy for authenticated users - higher limits
    options.AddPolicy("authenticated", httpContext =>
    {
        var userId = httpContext.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
        var endpoint = httpContext.Request.Path.Value ?? "unknown";

        // Partition by both user + endpoint
        var key = $"{userId}:{endpoint}";

        if (string.IsNullOrEmpty(userId) || userId == "anonymous")
        {
            // Lower limits for anonymous users
            return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: "anonymous",
                factory: partition => new FixedWindowRateLimiterOptions
                {
                    AutoReplenishment = true,
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(1)
                });
        }

        // Higher limits for authenticated users
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: key,
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1)
            });
    });

    // Sliding window policy for API-intensive operations
    options.AddPolicy("api-intensive", httpContext =>
    {
        var userId = httpContext.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
        var endpoint = httpContext.Request.Path.Value ?? "unknown";

        // Partition by both user + endpoint
        var key = $"{userId}:{endpoint}";

        return RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: key,
            factory: partition => new SlidingWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 6
            });
    });
});

var app = builder.Build();

app.UseCors("AllowReactApp");

// Configure the HTTP request pipeline.
// if (app.Environment.IsDevelopment())
// {
//     app.UseSwagger();
//     app.UseSwaggerUI();
// }

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseMiddleware<ExceptionHandlerMiddleware>();

app.UseAuthentication();

app.UseAuthorization();

app.UseRateLimiter();

app.MapControllers();

app.Run();
