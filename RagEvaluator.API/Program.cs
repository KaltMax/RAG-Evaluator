
namespace RagEvaluator.API
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            // Configure RAG Configuration
            var ragConfig = new Contract.Models.RagConfiguration();
            builder.Configuration.GetSection("RagConfiguration").Bind(ragConfig);
            builder.Services.AddSingleton(ragConfig);

            // Register logger wrapper
            builder.Services.AddSingleton(typeof(Contract.Logger.ILoggerWrapper<>), typeof(Contract.Logger.LoggerWrapper<>));

            // Register Infrastructure services (implementations)
            builder.Services.AddSingleton<Application.Services.Interfaces.IPdfLoader, Infrastructure.Services.PdfLoader>();
            builder.Services.AddSingleton<Application.Services.Interfaces.ITextChunker>(sp =>
                new Infrastructure.Services.TextChunker(ragConfig.ChunkSize, ragConfig.ChunkOverlap));
            builder.Services.AddSingleton<Application.Services.Interfaces.IVectorStore, Infrastructure.Services.SimpleVectorStore>();
            builder.Services.AddSingleton<Application.Services.Interfaces.IEmbeddingService, Infrastructure.Services.OllamaEmbeddingService>();
            builder.Services.AddSingleton<Application.Services.Interfaces.IChatService, Infrastructure.Services.OllamaChatService>();

            // Register Application services (business logic)
            builder.Services.AddSingleton<Application.Services.Interfaces.IRagService, Application.Services.RagService>();

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
