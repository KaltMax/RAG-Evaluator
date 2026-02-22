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
            if (!await _embeddingService.IsAvailableAsync(cancellationToken))
            {
                throw new InvalidOperationException("Embedding service not available. Ensure Ollama is running with the required model.");
            }

            // Load and extract text from PDF
            var pages = _pdfLoader.LoadPdf(pdfStream);
            var content = string.Join("\n\n", pages);
            _logger.LogInformation("Document {DocumentId}: extracted {PageCount} pages", documentId, pages.Count);

            // Split into chunks
            var textChunks = await _textChunker.CreateDocumentChunksAsync(content, cancellationToken);
            _logger.LogInformation("Document {DocumentId}: created {ChunkCount} chunks", documentId, textChunks.Count);

            // Generate embeddings and create chunk entities
            var documentChunks = new List<DocumentChunk>();
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

            await _documentChunkRepository.AddRangeAsync(documentChunks);

            // Update document status directly via repository
            var document = await _documentRepository.GetByIdAsync(documentId, cancellationToken);
            if (document is null)
            {
                throw new ArgumentException($"Document with id {documentId} not found");
            }

            document.Status = DocumentStatus.Completed;
            document.PageCount = pages.Count;
            document.ChunkCount = textChunks.Count;
            document.Content = content;
            document.ProcessedAt = DateTime.UtcNow;
            await _documentRepository.UpdateAsync(document, cancellationToken);

            _logger.LogInformation("Document {DocumentId}: processing completed", documentId);
        }

        public async Task<ReprocessResponse> ReprocessAllDocumentsAsync(CancellationToken cancellationToken = default)
        {
            if (!await _embeddingService.IsAvailableAsync(cancellationToken))
            {
                throw new InvalidOperationException("Embedding service not available. Ensure Ollama is running with the required model.");
            }

            // Delete all existing chunks before reprocessing
            await _documentChunkRepository.DeleteAllAsync(cancellationToken);

            var documents = await _documentRepository.GetByStatusAsync(DocumentStatus.Completed, cancellationToken);
            _logger.LogInformation("Reprocessing {DocumentCount} documents", documents.Count);
            var totalChunks = 0;
            var processed = 0;

            // Set all documents to Processing before starting
            foreach (var document in documents)
            {
                document.Status = DocumentStatus.Processing;
                await _documentRepository.UpdateAsync(document, cancellationToken);
            }

            // Reprocess each document with current chunking and embedding configuration
            foreach (var document in documents)
            {
                var textChunks = await _textChunker.CreateDocumentChunksAsync(document.Content!, cancellationToken);

                var documentChunks = new List<DocumentChunk>();
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
                        DocumentId = document.Id
                    });
                }

                // Save new chunks to repository
                await _documentChunkRepository.AddRangeAsync(documentChunks, cancellationToken);

                // Update document metadata and set status back to Completed
                document.Status = DocumentStatus.Completed;
                document.ChunkCount = documentChunks.Count;
                document.ProcessedAt = DateTime.UtcNow;
                await _documentRepository.UpdateAsync(document, cancellationToken);

                totalChunks += documentChunks.Count;
                processed++;
                _logger.LogInformation("Reprocessed document {Processed}/{Total}: {DocumentId} ({ChunkCount} chunks)",
                    processed, documents.Count, document.Id, documentChunks.Count);
            }

            return new ReprocessResponse
            {
                DocumentsProcessed = documents.Count,
                TotalChunksCreated = totalChunks,
                ChunkingStrategy = _config.ChunkingStrategy.ToString(),
                EmbeddingModel = _config.EmbeddingModel
            };
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
    }
}
