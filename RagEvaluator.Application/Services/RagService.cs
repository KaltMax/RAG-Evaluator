using RagEvaluator.Application.Services.Interfaces;
using RagEvaluator.Contract.Abstractions.Data;
using RagEvaluator.Contract.Abstractions.Services;
using RagEvaluator.Contract.Dtos.Responses;
using RagEvaluator.Contract.Configurations;
using RagEvaluator.Domain.Entities;

namespace RagEvaluator.Application.Services
{
    /// <summary>
    /// Main service for Retrieval-Augmented Generation operations
    /// Business logic orchestration layer
    /// </summary>
    public class RagService : IRagService
    {
        private readonly RagConfiguration _config;
        private readonly IPdfLoader _pdfLoader;
        private readonly ITextChunker _textChunker;
        private readonly IDocumentChunkRepository _documentChunkRepository;
        private readonly IEmbeddingService _embeddingService;
        private readonly IChatService _chatService;
        private readonly IDocumentService _documentService;
        private readonly IMetricsService _metricsService;

        public RagService(
            RagConfiguration config,
            IPdfLoader pdfLoader,
            ITextChunker textChunker,
            IDocumentChunkRepository documentChunkRepository,
            IEmbeddingService embeddingService,
            IChatService chatService,
            IDocumentService documentService,
            IMetricsService metricsService)
        {
            _config = config;
            _pdfLoader = pdfLoader;
            _textChunker = textChunker;
            _documentChunkRepository = documentChunkRepository;
            _embeddingService = embeddingService;
            _chatService = chatService;
            _documentService = documentService;
            _metricsService = metricsService;
        }

        public async Task<DocumentResponse> ProcessDocumentAsync(Stream pdfStream, string fileName, string language)
        {
            if (!await _embeddingService.IsAvailableAsync())
            {
                throw new InvalidOperationException("Embedding service not available. Ensure Ollama is running with the required models.");
            }

            // Create document with Pending status
            var document = await _documentService.CreateDocumentAsync(pdfStream, fileName, pdfStream.Length, "application/pdf", language);

            try
            {
                // Update status to Processing
                await _documentService.UpdateStatusAsync(document.Id, DocumentStatus.Processing);

                pdfStream.Position = 0;

                // Load and process PDF
                var pages = _pdfLoader.LoadPdf(pdfStream);
                var chunks = _textChunker.SplitDocuments(pages);
                var content = string.Join("\n\n", pages);

                // Generate embeddings and store chunks
                var documentChunks = new List<DocumentChunk>();
                foreach (var chunk in chunks)
                {
                    var embedding = await _embeddingService.GenerateEmbeddingAsync(chunk);
                    documentChunks.Add(new DocumentChunk
                    {
                        Id = Guid.NewGuid(),
                        Text = chunk,
                        Embedding = embedding,
                        ChunkingStrategy = "fixed-size", // TODO: Make configurable
                        EmbeddingModel = _config.EmbeddingModel,
                        DocumentId = document.Id
                    });
                }
                await _documentChunkRepository.AddRangeAsync(documentChunks);

                // Update status to Completed with page count, chunk count, and content
                await _documentService.UpdateStatusAsync(document.Id, DocumentStatus.Completed, pages.Count, chunks.Count, content);

                return new DocumentResponse
                {
                    Id = document.Id,
                    FileName = fileName,
                    Language = language,
                    PageCount = pages.Count,
                    ChunkCount = chunks.Count,
                    UploadedAt = document.UploadedAt,
                    Status = DocumentStatus.Completed.ToString()
                };
            }
            catch
            {
                // Update status to Failed on error
                await _documentService.UpdateStatusAsync(document.Id, DocumentStatus.Failed);
                throw;
            }
        }

        public async Task<QueryResponse> AskQuestionAsync(string question, int topK = 3)
        {
            if (!await _chatService.IsAvailableAsync() || !await _embeddingService.IsAvailableAsync())
            {
                throw new InvalidOperationException("RAG services not available. Ensure Ollama is running with the required models.");
            }

            var queryId = Guid.NewGuid();

            // Generate embedding for the question
            var questionEmbedding = await _embeddingService.GenerateEmbeddingAsync(question);

            // Search for relevant document chunks
            var chunkMatches = await _documentChunkRepository.SearchAsync(questionEmbedding, topK);

            if (chunkMatches.Count == 0)
            {
                return new QueryResponse
                {
                    QueryId = queryId,
                    Question = question,
                    Answer = "No relevant documents found in the knowledge base. Please upload documents first.",
                    Sources = new List<SearchResultDto>()
                };
            }

            // Build context from search results
            var context = string.Join("\n\n", chunkMatches.Select(r => r.Text));

            // Generate answer using LLM
            var userMessage = $"Context:\n{context}\n\nQuestion: {question}\n\nAnswer:";
            var answer = await _chatService.GenerateResponseAsync(_config.SystemPrompt, userMessage);

            // Convert chunk matches to DTOs with similarity calculated by MetricsService
            var sourceDtos = chunkMatches.Select(match => new SearchResultDto
            {
                Id = match.Id,
                Text = match.Text,
                Similarity = _metricsService.CosineSimilarity(questionEmbedding, match.Embedding),
                DocumentId = match.DocumentId,
                FileName = match.FileName,
                ChunkingStrategy = match.ChunkingStrategy,
                EmbeddingModel = match.EmbeddingModel
            }).ToList();

            return new QueryResponse
            {
                QueryId = queryId,
                Question = question,
                Answer = answer,
                Sources = sourceDtos
            };
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
