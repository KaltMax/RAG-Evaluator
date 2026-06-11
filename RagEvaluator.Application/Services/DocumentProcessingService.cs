using Microsoft.Extensions.Logging;
using RagEvaluator.Application.Mappers;
using RagEvaluator.Application.Services.Interfaces;
using RagEvaluator.Contract.Abstractions.Data;
using RagEvaluator.Contract.Abstractions.Services;
using RagEvaluator.Contract.Configurations;
using RagEvaluator.Contract.Dtos.Responses;
using RagEvaluator.Domain.Entities;
using RagEvaluator.Domain.Enums;
using RagEvaluator.Domain.ValueObjects;

namespace RagEvaluator.Application.Services
{
    /// <summary>
    /// Service for document processing operations including PDF text extraction, chunking, embedding, and chunk search.
    /// </summary>
    public class DocumentProcessingService : IDocumentProcessingService
    {
        private readonly ILogger<DocumentProcessingService> _logger;
        private readonly IDocumentRepository _documentRepository;
        private readonly IDocumentChunkRepository _documentChunkRepository;
        private readonly IPdfLoader _pdfLoader;
        private readonly ITextChunker _textChunker;
        private readonly IEmbeddingService _embeddingService;
        private readonly RagConfiguration _config;

        public DocumentProcessingService(
            ILogger<DocumentProcessingService> logger,
            IDocumentRepository documentRepository,
            IDocumentChunkRepository documentChunkRepository,
            IPdfLoader pdfLoader,
            ITextChunker textChunker,
            IEmbeddingService embeddingService,
            RagConfiguration config)
        {
            _logger = logger;
            _documentRepository = documentRepository;
            _documentChunkRepository = documentChunkRepository;
            _pdfLoader = pdfLoader;
            _textChunker = textChunker;
            _embeddingService = embeddingService;
            _config = config;
        }

        public async Task ProcessDocumentContentAsync(Guid documentId, Stream pdfStream, CancellationToken cancellationToken = default)
        {
            await EnsureEmbeddingServiceAvailableAsync(cancellationToken);

            // Load and extract text from PDF
            var pages = _pdfLoader.LoadPdf(pdfStream);
            var content = string.Join("\n\n", pages);
            _logger.LogInformation("Document {DocumentId}: extracted {PageCount} pages", documentId, pages.Count);

            var documentChunks = await BuildChunksAsync(documentId, content, cancellationToken);
            await _documentChunkRepository.AddRangeAsync(documentChunks, cancellationToken);

            // Update document status directly via repository
            var document = await _documentRepository.GetByIdAsync(documentId, cancellationToken);
            if (document is null)
            {
                throw new ArgumentException($"Document with id {documentId} not found");
            }

            document.Status = DocumentStatus.Completed;
            document.PageCount = pages.Count;
            document.ChunkCount = documentChunks.Count;
            document.Content = content;
            document.ProcessedAt = DateTime.UtcNow;
            await _documentRepository.UpdateAsync(document, cancellationToken);

            _logger.LogInformation("Document {DocumentId}: processing completed", documentId);
        }

        public async Task<ReprocessResponse> ReprocessAllDocumentsAsync(CancellationToken cancellationToken = default)
        {
            await EnsureEmbeddingServiceAvailableAsync(cancellationToken);

            // Reprocess any document that has extracted content, regardless of status — this also recovers
            // documents left Failed by a previous run or stuck Processing by an aborted one.
            var documents = (await _documentRepository.GetAllAsync(cancellationToken))
                .Where(d => !string.IsNullOrEmpty(d.Content))
                .ToList();
            _logger.LogInformation("Reprocessing {DocumentCount} documents", documents.Count);

            // Mark all as Processing up front so a refresh reflects that reprocessing is ongoing.
            await _documentRepository.SetStatusAsync(documents.Select(d => d.Id), DocumentStatus.Processing, cancellationToken);

            var totalChunks = 0;
            var failed = 0;
            var processed = 0;

            // Reprocess each document independently so one failure does not abort the rest.
            foreach (var document in documents)
            {
                processed++;
                try
                {
                    var chunkCount = await ReprocessDocumentAsync(document, cancellationToken);
                    totalChunks += chunkCount;
                    _logger.LogInformation("Reprocessed document {Processed}/{Total}: {DocumentId} ({ChunkCount} chunks)",
                        processed, documents.Count, document.Id, chunkCount);
                }
                catch (Exception ex)
                {
                    failed++;
                    _logger.LogError(ex, "Failed to reprocess document {Processed}/{Total}: {DocumentId}",
                        processed, documents.Count, document.Id);
                    document.Status = DocumentStatus.Failed;
                    await _documentRepository.UpdateAsync(document, cancellationToken);
                }
            }

            return new ReprocessResponse
            {
                DocumentsProcessed = documents.Count,
                DocumentsFailed = failed,
                TotalChunksCreated = totalChunks,
                ChunkingStrategy = _config.ChunkingStrategy.ToString(),
                EmbeddingModel = _config.EmbeddingModel
            };
        }

        private async Task<int> ReprocessDocumentAsync(Document document, CancellationToken cancellationToken)
        {
            // Build new chunks first; the old chunks stay queryable until the swap below.
            var documentChunks = await BuildChunksAsync(document.Id, document.Content!, cancellationToken);

            await _documentChunkRepository.DeleteByDocumentIdAsync(document.Id, cancellationToken);
            await _documentChunkRepository.AddRangeAsync(documentChunks, cancellationToken);

            document.Status = DocumentStatus.Completed;
            document.ChunkCount = documentChunks.Count;
            document.ProcessedAt = DateTime.UtcNow;
            await _documentRepository.UpdateAsync(document, cancellationToken);

            return documentChunks.Count;
        }

        public async Task<IReadOnlyList<DocumentChunkResponse>> GetChunksByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default)
        {
            var chunks = await _documentChunkRepository.GetByDocumentIdAsync(documentId, cancellationToken);
            return chunks.ToResponseList();
        }

        public async Task<IReadOnlyList<ChunkSearchMatch>> SearchChunksAsync(float[] queryEmbedding, int topK, CancellationToken cancellationToken = default)
        {
            return await _documentChunkRepository.SearchAsync(queryEmbedding, topK, cancellationToken);
        }

        private async Task EnsureEmbeddingServiceAvailableAsync(CancellationToken cancellationToken)
        {
            if (!await _embeddingService.IsAvailableAsync(cancellationToken))
            {
                throw new InvalidOperationException("Embedding service not available. Ensure Ollama is running with the required model.");
            }
        }

        private async Task<List<DocumentChunk>> BuildChunksAsync(Guid documentId, string content, CancellationToken cancellationToken)
        {
            var textChunks = await _textChunker.CreateDocumentChunksAsync(content, cancellationToken);
            _logger.LogInformation("Document {DocumentId}: created {ChunkCount} chunks", documentId, textChunks.Count);

            var documentChunks = new List<DocumentChunk>(textChunks.Count);
            foreach (var chunkText in textChunks)
            {
                var embedding = await _embeddingService.GenerateDocumentEmbeddingAsync(chunkText, cancellationToken);
                documentChunks.Add(new DocumentChunk
                {
                    Id = Guid.NewGuid(),
                    Text = chunkText,
                    Embedding = embedding,
                    ChunkingStrategy = _config.ChunkingStrategy.ToString(),
                    EmbeddingModel = _config.EmbeddingModel,
                    DocumentId = documentId
                });
            }

            return documentChunks;
        }
    }
}
