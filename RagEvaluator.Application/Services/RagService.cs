using System.Diagnostics;
using RagEvaluator.Application.Mappers;
using RagEvaluator.Application.Services.Interfaces;
using RagEvaluator.Contract.Abstractions.Services;
using RagEvaluator.Contract.Configurations;
using RagEvaluator.Contract.Dtos.Requests;
using RagEvaluator.Contract.Dtos.Responses;
using RagEvaluator.Domain.Enums;

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

        public RagService(
            RagConfiguration config,
            IChatService chatService,
            IDocumentService documentService,
            IQueryService queryService)
        {
            _config = config;
            _chatService = chatService;
            _documentService = documentService;
            _queryService = queryService;
        }

        public async Task<DocumentResponse> ProcessDocumentAsync(Stream documentStream, string fileName, string contentType, string language, CancellationToken cancellationToken = default)
        {
            // Create document with Pending status
            var document = await _documentService.CreateDocumentAsync(documentStream, fileName, documentStream.Length, contentType, language, cancellationToken);

            try
            {
                // Update status to Processing
                await _documentService.UpdateStatusAsync(document.Id, DocumentStatus.Processing, cancellationToken: cancellationToken);

                // Process document content (PDF → chunks → embeddings → store → Completed)
                documentStream.Position = 0;
                await _documentService.ProcessDocumentContentAsync(document.Id, documentStream, cancellationToken);

                // Return updated document
                return (await _documentService.GetByIdAsync(document.Id, cancellationToken))!;
            }
            catch
            {
                // Update status to Failed on error
                await _documentService.UpdateStatusAsync(document.Id, DocumentStatus.Failed);
                throw;
            }
        }

        public async Task<QueryResponse> AskQuestionAsync(AskQuestionRequest request, CancellationToken cancellationToken = default)
        {
            if (!await _chatService.IsAvailableAsync(cancellationToken) || !await _queryService.IsReadyAsync(cancellationToken))
            {
                throw new InvalidOperationException("RAG services not available. Ensure Ollama is running with the required models.");
            }

            var stopwatch = Stopwatch.StartNew();

            // Resolve prompt from template + query language
            var systemPrompt = PromptTemplateResolver.Resolve(_config.PromptTemplate, request.Language, _config);

            // Create query object with configuration snapshot
            var query = await _queryService.CreateQueryAsync(
                request.Question,
                request.Language,
                request.TopK,
                systemPrompt,
                _config.ChunkingStrategy.ToString(),
                _config.EmbeddingModel,
                _config.ChatModel,
                cancellationToken);

            // Search for relevant document chunks
            var chunkMatches = await _documentService.SearchChunksAsync(query.QueryEmbedding, query.TopK, cancellationToken);

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
                answer = await _chatService.GenerateResponseAsync(systemPrompt, userMessage, cancellationToken);
            }

            // Calculate response time
            stopwatch.Stop();
            var responseTimeMs = (int)stopwatch.ElapsedMilliseconds;

            // Persist query results (answer, retrieved chunks)
            await _queryService.CompleteQueryAsync(query, answer, responseTimeMs, chunkMatches, cancellationToken);

            // Map results from persisted QueryResults (to get correct QueryResult IDs for annotation)
            var sources = query.Results.ToSearchResultDtoList();

            return query.ToResponse(answer, sources);
        }

        public async Task<bool> IsInitializedAsync(CancellationToken cancellationToken = default)
        {
            return await _queryService.IsReadyAsync(cancellationToken) && await _chatService.IsAvailableAsync(cancellationToken);
        }
    }
}
