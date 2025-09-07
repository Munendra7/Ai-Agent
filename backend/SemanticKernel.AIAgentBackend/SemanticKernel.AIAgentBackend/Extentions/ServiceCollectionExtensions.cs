using SemanticKernel.AIAgentBackend.Factories.Factory;
using SemanticKernel.AIAgentBackend.Factories.Interface;
using SemanticKernel.AIAgentBackend.Repositories.Interface;
using SemanticKernel.AIAgentBackend.Repositories.Repository;

namespace SemanticKernel.AIAgentBackend.Extentions.cs
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSemanticKernelServices(this IServiceCollection services, IConfiguration config)
        {
            services.AddScoped<IKernelFactory, KernelFactory>();
            services.AddScoped<IEmbeddingKernelFactory, EmbeddingKernelFactory>();
            services.AddScoped<IDocumentsProcessFactory, DocumentsProcessFactory>();
            services.AddScoped<IAgentFactory, AgentFactory>();

            services.AddScoped<IChatHistoryService, ChatHistoryService>();
            services.AddScoped<IEmbeddingService, EmbeddingService>();
            services.AddScoped<IBlobService, BlobService>();
            services.AddScoped<IVideoProcessingService, VideoProcessingService>();

            return services;
        }
    }
}
