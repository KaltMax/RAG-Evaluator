
namespace RagEvaluator.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            // Configure RAG Configuration
            var ragConfig = new RagEvaluator.Contract.Models.RagConfiguration();
            builder.Configuration.GetSection("RagConfiguration").Bind(ragConfig);
            builder.Services.AddSingleton(ragConfig);

            // Register logger wrapper
            builder.Services.AddSingleton(typeof(RagEvaluator.Contract.Logger.ILoggerWrapper<>), typeof(RagEvaluator.Contract.Logger.LoggerWrapper<>));

            // Register Infrastructure services (implementations)
            builder.Services.AddSingleton<RagEvaluator.Application.Services.Interfaces.IPdfLoader, RagEvaluator.Infrastructure.Services.PdfLoader>();
            builder.Services.AddSingleton<RagEvaluator.Application.Services.Interfaces.ITextChunker>(sp =>
                new RagEvaluator.Infrastructure.Services.TextChunker(ragConfig.ChunkSize, ragConfig.ChunkOverlap));
            builder.Services.AddSingleton<RagEvaluator.Application.Services.Interfaces.IVectorStore, RagEvaluator.Infrastructure.Services.SimpleVectorStore>();
            builder.Services.AddSingleton<RagEvaluator.Application.Services.Interfaces.IEmbeddingService, RagEvaluator.Infrastructure.Services.OllamaEmbeddingService>();
            builder.Services.AddSingleton<RagEvaluator.Application.Services.Interfaces.IChatService, RagEvaluator.Infrastructure.Services.OllamaChatService>();

            // Register Application services (business logic)
            builder.Services.AddSingleton<RagEvaluator.Application.Services.Interfaces.IRagService, RagEvaluator.Application.Services.RagService>();

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
            // Enable Swagger in all environments for testing/demo purposes
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "RAG Evaluator API v1");
                options.RoutePrefix = "swagger";
            });

            app.UseHttpsRedirection();

            app.UseCors();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
