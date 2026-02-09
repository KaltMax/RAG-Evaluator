using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using RagEvaluator.Application.Services;
using RagEvaluator.Application.Services.Interfaces;
using RagEvaluator.Contract.Abstractions.Data;
using RagEvaluator.Contract.Abstractions.Services;
using RagEvaluator.Contract.Logger;
using RagEvaluator.Infrastructure.Data;
using RagEvaluator.Infrastructure.Data.Repositories;
using RagEvaluator.Infrastructure.Services;

namespace RagEvaluator.API
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            // FileStorage Configuration
            var fileStorageConfig = new Contract.Configurations.FileStorageConfiguration();
            builder.Configuration.GetSection("FileStorageConfiguration").Bind(fileStorageConfig);
            builder.Services.AddSingleton(fileStorageConfig);

            // RAG Configuration
            var ragConfig = new Contract.Configurations.RagConfiguration();
            builder.Configuration.GetSection("RagConfiguration").Bind(ragConfig);

            // Default active embedding model to first in the available list
            var availableModels = ragConfig.AvailableEmbeddingModels
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (availableModels.Length > 0)
                ragConfig.EmbeddingModel = availableModels[0];

            builder.Services.AddSingleton(ragConfig);

            // Register logger wrapper
            builder.Services.AddSingleton(typeof(ILoggerWrapper<>), typeof(LoggerWrapper<>));

            // Register DbContext with PostgreSQL and pgvector support
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(
                    builder.Configuration.GetConnectionString("DefaultConnection"),
                    o => o.UseVector()));

            // Register repositories
            builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
            builder.Services.AddScoped<IDocumentChunkRepository, DocumentChunkRepository>();
            builder.Services.AddScoped<IQueryRepository, QueryRepository>();
            builder.Services.AddScoped<IExperimentRepository, ExperimentRepository>();

            // Register Infrastructure services (implementations)
            builder.Services.AddSingleton<IPdfLoader, PdfPigLoader>();
            builder.Services.AddSingleton<FixedSizeTextChunker>();
            builder.Services.AddSingleton<SemanticTextChunker>();
            builder.Services.AddTransient<ITextChunker>(sp =>
                sp.GetRequiredService<Contract.Configurations.RagConfiguration>().ChunkingStrategy == Domain.Enums.ChunkingStrategy.Semantic
                    ? sp.GetRequiredService<SemanticTextChunker>()
                    : sp.GetRequiredService<FixedSizeTextChunker>());
            builder.Services.AddSingleton<IEmbeddingService, OllamaEmbeddingService>();
            builder.Services.AddSingleton<IChatService, OllamaChatService>();
            builder.Services.AddSingleton<IFileStorageService, LocalFileStorageService>();

            // Register Application services (business logic)
            builder.Services.AddScoped<IDocumentService, DocumentService>();
            builder.Services.AddScoped<IQueryService, QueryService>();
            builder.Services.AddScoped<IRagService, RagService>();
            builder.Services.AddSingleton<ISettingsService, SettingsService>();
            builder.Services.AddSingleton<IMetricsService, MetricsService>();
            builder.Services.AddScoped<IExperimentService, ExperimentService>();

            // Experiment background processing
            builder.Services.AddSingleton<ExperimentQueue>();
            builder.Services.AddHostedService<ExperimentBackgroundService>();

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

            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                });

            // Add Swagger services
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "RAG Evaluator API",
                    Version = "v1",
                    Description = "API for RAG document processing and querying"
                });
            });

            var app = builder.Build();

            // Apply pending EF Core migrations at startup (development only)
            if (app.Environment.IsDevelopment())
            {
                using var scope = app.Services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.Database.Migrate();
            }

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
