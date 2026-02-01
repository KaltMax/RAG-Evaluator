using RagEvaluator.Application.Mappers;
using RagEvaluator.Application.Services.Interfaces;
using RagEvaluator.Contract.Abstractions.Services;
using RagEvaluator.Contract.Dtos.Requests;
using RagEvaluator.Contract.Dtos.Responses;
using RagEvaluator.Contract.Configurations;
using RagEvaluator.Domain.Entities;

namespace RagEvaluator.Application.Services
{
    /// <summary>
    /// Orchestrator for Retrieval-Augmented Generation operations.
    /// Coordinates document processing and query answering workflows.
    /// </summary>
    public class RagService : IRagService
    {
        private readonly RagConfiguration _config;
        private readonly IEmbeddingService _embeddingService;
        private readonly IChatService _chatService;
        private readonly IDocumentService _documentService;
        private readonly IQueryService _queryService;
        private readonly IMetricsService _metricsService;

        public RagService(
            RagConfiguration config,
            IEmbeddingService embeddingService,
            IChatService chatService,
            IDocumentService documentService,
            IQueryService queryService,
            IMetricsService metricsService)
        {
            _config = config;
            _embeddingService = embeddingService;
            _chatService = chatService;
            _documentService = documentService;
            _queryService = queryService;
            _metricsService = metricsService;
        }

        public async Task<DocumentResponse> ProcessDocumentAsync(Stream pdfStream, string fileName, string language)
        {
            // Create document with Pending status
            var document = await _documentService.CreateDocumentAsync(pdfStream, fileName, pdfStream.Length, "application/pdf", language);

            try
            {
                // Update status to Processing
                await _documentService.UpdateStatusAsync(document.Id, DocumentStatus.Processing);

                // Process document content (PDF → chunks → embeddings → store → Completed)
                pdfStream.Position = 0;
                await _documentService.ProcessDocumentContentAsync(document.Id, pdfStream);

                // Return updated document
                return (await _documentService.GetByIdAsync(document.Id))!;
            }
            catch
            {
                // Update status to Failed on error
                await _documentService.UpdateStatusAsync(document.Id, DocumentStatus.Failed);
                throw;
            }
        }

        public async Task<QueryResponse> AskQuestionAsync(AskQuestionRequest request)
        {
            if (!await _chatService.IsAvailableAsync() || !await _embeddingService.IsAvailableAsync())
            {
                throw new InvalidOperationException("RAG services not available. Ensure Ollama is running with the required models.");
            }

            // Create and persist the query with configuration snapshot
            var query = await _queryService.CreateQueryAsync(
                request.Question,
                request.Language,
                request.TopK,
                _config.SystemPrompt,
                _config.EmbeddingModel,
                _config.ChatModel);

            // Generate embedding for the question
            var questionEmbedding = await _embeddingService.GenerateEmbeddingAsync($"search_query: {request.Question}");

            // Search for relevant document chunks
            var chunkMatches = await _documentService.SearchChunksAsync(questionEmbedding, request.TopK);

            if (chunkMatches.Count == 0)
            {
                return query.ToResponse(
                    "No relevant documents found in the knowledge base. Please upload documents first.",
                    []);
            }

            // Build context from search results
            var context = string.Join("\n\n", chunkMatches.Select(r => r.Text));

            // Generate answer using LLM
            var userMessage = $"Context:\n{context}\n\nQuestion: {request.Question}\n\nAnswer:";
            var answer = await _chatService.GenerateResponseAsync(_config.SystemPrompt, userMessage);

            // Map results using QueryMapper
            var sources = chunkMatches.ToSearchResultDtoList(questionEmbedding, _metricsService.CosineSimilarity);

            return query.ToResponse(answer, sources);
        }

        public async Task<bool> IsInitializedAsync()
        {
            return await _embeddingService.IsAvailableAsync() && await _chatService.IsAvailableAsync();
        }

        public async Task<int> GetDocumentCountAsync()
        {
            var documents = await _documentService.GetAllAsync();
            return documents.Count;
        }
    }
}
