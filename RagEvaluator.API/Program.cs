using RagEvaluator.Application.Services;
using RagEvaluator.Application.Services.Interfaces;
using RagEvaluator.Contract.Abstractions.Data;
using RagEvaluator.Contract.Abstractions.Services;
using RagEvaluator.Contract.Logger;
using RagEvaluator.Infrastructure.Services;

namespace RagEvaluator.API
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            // Configure RAG Configuration
            var ragConfig = new Contract.Configurations.RagConfiguration();
            builder.Configuration.GetSection("RagConfiguration").Bind(ragConfig);
            builder.Services.AddSingleton(ragConfig);

            // Register logger wrapper
            builder.Services.AddSingleton(typeof(ILoggerWrapper<>), typeof(LoggerWrapper<>));

            // Register Infrastructure services (implementations)
            builder.Services.AddSingleton<IPdfLoader, PdfLoader>();
            builder.Services.AddSingleton<ITextChunker>(sp =>
                new TextChunker(ragConfig.ChunkSize, ragConfig.ChunkOverlap));
            builder.Services.AddSingleton<IVectorStore, SimpleVectorStore>();
            builder.Services.AddSingleton<IEmbeddingService, OllamaEmbeddingService>();
            builder.Services.AddSingleton<IChatService, OllamaChatService>();

            // Register Application services (business logic)
            builder.Services.AddSingleton<IRagService, RagService>();

            // Add CORS for development
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            builder.Services.AddControllers();

            // Add Swagger services
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = "RAG Evaluator API",
                    Version = "v1",
                    Description = "API for Simple-RAG document processing and querying"
                });
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseCors();
            app.UseAuthorization();
            app.MapControllers();
            app.Run();
        }
    }
}
