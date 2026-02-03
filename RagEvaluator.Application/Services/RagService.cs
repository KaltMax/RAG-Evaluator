using System.Diagnostics;
using RagEvaluator.Application.Mappers;
using RagEvaluator.Application.Services.Interfaces;
using RagEvaluator.Contract.Abstractions.Services;
using RagEvaluator.Contract.Configurations;
using RagEvaluator.Contract.Dtos.Requests;
using RagEvaluator.Contract.Dtos.Responses;
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
        private readonly IChatService _chatService;
        private readonly IDocumentService _documentService;
        private readonly IQueryService _queryService;
        private readonly IMetricsService _metricsService;

        public RagService(
            RagConfiguration config,
            IChatService chatService,
            IDocumentService documentService,
            IQueryService queryService,
            IMetricsService metricsService)
        {
            _config = config;
            _chatService = chatService;
            _documentService = documentService;
            _queryService = queryService;
            _metricsService = metricsService;
        }

        public async Task<DocumentResponse> ProcessDocumentAsync(Stream documentStream, string fileName, string contentType, string language)
        {
            // Create document with Pending status
            var document = await _documentService.CreateDocumentAsync(documentStream, fileName, documentStream.Length, contentType, language);

            try
            {
                // Update status to Processing
                await _documentService.UpdateStatusAsync(document.Id, DocumentStatus.Processing);

                // Process document content (PDF → chunks → embeddings → store → Completed)
                documentStream.Position = 0;
                await _documentService.ProcessDocumentContentAsync(document.Id, documentStream);

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
            if (!await _chatService.IsAvailableAsync() || !await _queryService.IsReadyAsync())
            {
                throw new InvalidOperationException("RAG services not available. Ensure Ollama is running with the required models.");
            }

            var stopwatch = Stopwatch.StartNew();

            // Create query object with configuration snapshot
            var query = await _queryService.CreateQueryAsync(
                request.Question,
                request.Language,
                request.TopK,
                _config.SystemPrompt,
                _config.ChunkingStrategy,
                _config.EmbeddingModel,
                _config.ChatModel);

            // Search for relevant document chunks
            var chunkMatches = await _documentService.SearchChunksAsync(query.QueryEmbedding, query.TopK);

            string answer;
            if (chunkMatches.Count == 0)
            {
                answer = "No relevant documents found in the knowledge base. Please upload documents first.";
            }
            else
            {
                // Build context from search results
                var context = string.Join("\n\n", chunkMatches.Select(r => r.Text));

                // Generate answer using LLM
                var userMessage = $"Context:\n{context}\n\nQuestion: {request.Question}\n\nAnswer:";
                answer = await _chatService.GenerateResponseAsync(_config.SystemPrompt, userMessage);
            }

            // Calculate response time
            stopwatch.Stop();
            var responseTimeMs = (int)stopwatch.ElapsedMilliseconds;

            // Persist query results (answer, retrieved chunks)
            await _queryService.CompleteQueryAsync(query, answer, responseTimeMs, chunkMatches);

            // Map results using QueryMapper
            var sources = chunkMatches.ToSearchResultDtoList(query.QueryEmbedding, _metricsService.CosineSimilarity);

            return query.ToResponse(answer, sources);
        }

        public async Task<bool> IsInitializedAsync()
        {
            return await _queryService.IsReadyAsync() && await _chatService.IsAvailableAsync();
        }

        public async Task<int> GetDocumentCountAsync()
        {
            var documents = await _documentService.GetAllAsync();
            return documents.Count;
        }
    }
}
