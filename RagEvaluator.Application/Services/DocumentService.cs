using Microsoft.Extensions.Logging;
using RagEvaluator.Application.Mappers;
using RagEvaluator.Application.Services.Interfaces;
using RagEvaluator.Application.Workers;
using RagEvaluator.Contract.Abstractions.BackgroundProcessing;
using RagEvaluator.Contract.Abstractions.Data;
using RagEvaluator.Contract.Abstractions.Services;
using RagEvaluator.Contract.Configurations;
using RagEvaluator.Contract.Dtos.Responses;
using RagEvaluator.Domain.Entities;
using RagEvaluator.Domain.Enums;

namespace RagEvaluator.Application.Services
{
    /// <summary>
    /// Document feature service: upload orchestration, PDF processing (extract → chunk → embed),
    /// reprocessing, CRUD, file storage, and chunk retrieval.
    /// </summary>
    public class DocumentService : IDocumentService
    {
        private readonly ILogger<DocumentService> _logger;
        private readonly IDocumentRepository _documentRepository;
        private readonly IDocumentChunkRepository _documentChunkRepository;
        private readonly IFileStorageService _fileStorageService;
        private readonly IPdfLoader _pdfLoader;
        private readonly ITextChunker _textChunker;
        private readonly IEmbeddingService _embeddingService;
        private readonly IBackgroundTaskQueue<DocumentProcessingJob> _documentQueue;
        private readonly IBackgroundTaskQueue<DocumentReprocessingJob> _reprocessQueue;
        private readonly RagConfiguration _config;

        public DocumentService(
            ILogger<DocumentService> logger,
            IDocumentRepository documentRepository,
            IDocumentChunkRepository documentChunkRepository,
            IFileStorageService fileStorageService,
            IPdfLoader pdfLoader,
            ITextChunker textChunker,
            IEmbeddingService embeddingService,
            IBackgroundTaskQueue<DocumentProcessingJob> documentQueue,
            IBackgroundTaskQueue<DocumentReprocessingJob> reprocessQueue,
            RagConfiguration config)
        {
            _logger = logger;
            _documentRepository = documentRepository;
            _documentChunkRepository = documentChunkRepository;
            _fileStorageService = fileStorageService;
            _pdfLoader = pdfLoader;
            _textChunker = textChunker;
            _embeddingService = embeddingService;
            _documentQueue = documentQueue;
            _reprocessQueue = reprocessQueue;
            _config = config;
        }

        // ---- CRUD ----

        public async Task<DocumentResponse> CreateDocumentAsync(Stream fileStream, string fileName, string mimeType, string language, string course, CancellationToken cancellationToken = default)
        {
            fileName = Path.GetFileName(fileName);

            var existing = await _documentRepository.GetByNameAsync(fileName, cancellationToken);
            if (existing is not null)
            {
                throw new ArgumentException($"A document with the name '{fileName}' already exists.");
            }

            var document = new Document
            {
                Id = Guid.NewGuid(),
                FileName = fileName,
                FilePath = null,
                FileSize = fileStream.Length,
                MimeType = mimeType,
                Language = language,
                Course = course,
                UploadedAt = DateTime.UtcNow,
                Status = DocumentStatus.Pending
            };

            // Save file to storage
            var filePath = await _fileStorageService.SaveFileAsync(fileStream, document.Id, fileName, cancellationToken);
            document.FilePath = filePath;

            // Save document metadata to repository. If persistence fails remove the just-saved file so it is not left orphaned.
            try
            {
                await _documentRepository.AddAsync(document, cancellationToken);
            }
            catch
            {
                await _fileStorageService.DeleteFileAsync(filePath, CancellationToken.None);
                throw;
            }

            await _documentQueue.EnqueueAsync(new DocumentProcessingJob(document.Id), cancellationToken);

            return document.ToResponse();
        }

        public async Task<DocumentResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var document = await _documentRepository.GetByIdAsync(id, cancellationToken);
            return document?.ToResponse();
        }

        public async Task<DocumentResponse?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            var document = await _documentRepository.GetByNameAsync(name, cancellationToken);
            return document?.ToResponse();
        }

        public async Task<IReadOnlyList<DocumentResponse>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var summaries = await _documentRepository.GetAllSummariesAsync(cancellationToken);
            return summaries.ToResponseList();
        }

        public async Task<DocumentFileInfo?> GetDocumentFileInfoAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var document = await _documentRepository.GetByIdAsync(id, cancellationToken);
            if (document?.FilePath is null)
            {
                return null;
            }

            return new DocumentFileInfo
            {
                FilePath = document.FilePath,
                FileName = document.FileName,
                MimeType = document.MimeType ?? "application/pdf"
            };
        }

         public async Task<IReadOnlyList<DocumentChunkResponse>> GetChunksByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default)
        {
            var chunks = await _documentChunkRepository.GetByDocumentIdAsync(documentId, cancellationToken);
            return chunks.ToResponseList();
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var document = await _documentRepository.GetByIdAsync(id, cancellationToken);

            if (document?.FilePath != null)
            {
                await _fileStorageService.DeleteFileAsync(document.FilePath, CancellationToken.None);
            }
            await _documentChunkRepository.DeleteByDocumentIdAsync(id, CancellationToken.None);
            await _documentRepository.DeleteAsync(id, CancellationToken.None);
        }

        public Task SetStatusAsync(Guid documentId, DocumentStatus status, CancellationToken cancellationToken = default)
        {
            return _documentRepository.SetStatusAsync(documentId, status, cancellationToken);
        }

        // ---- Processing ----

        public async Task ProcessDocumentAsync(Guid documentId, string filePath, CancellationToken cancellationToken = default)
        {
            await EnsureEmbeddingServiceAvailableAsync(cancellationToken);

            // Load and extract text from the stored PDF.
            await using var stream = await _fileStorageService.OpenReadFileAsync(filePath, cancellationToken);
            var pages = _pdfLoader.LoadPdf(stream);
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

        public async Task ReprocessDocumentAsync(Guid documentId, CancellationToken cancellationToken)
        {
            var document = await _documentRepository.GetByIdAsync(documentId, cancellationToken);
            if (document?.Content is null)
            {
                throw new ArgumentException($"Document with id {documentId} not found or has no content for reprocessing");
            }

            // Build new chunks first; the old chunks stay queryable until the atomic swap below.
            var documentChunks = await BuildChunksAsync(document.Id, document.Content, cancellationToken);

            await _documentChunkRepository.ReplaceChunksAsync(document.Id, documentChunks, cancellationToken);

            document.Status = DocumentStatus.Completed;
            document.ChunkCount = documentChunks.Count;
            document.ProcessedAt = DateTime.UtcNow;
            await _documentRepository.UpdateAsync(document, cancellationToken);

            _logger.LogInformation("Document {DocumentId}: reprocessing completed", documentId);
        }

        public async Task<ReprocessResponse> ReprocessAllDocumentsAsync(CancellationToken cancellationToken = default)
        {
            // Fail fast: reject the whole request if embeddings can't be generated.
            await EnsureEmbeddingServiceAvailableAsync(cancellationToken);

            // Reprocess any document that has extracted content, regardless of status.
            var ids = (await _documentRepository.GetReprocessableAsync(cancellationToken))
                .Select(d => d.Id)
                .ToList();
            _logger.LogInformation("Queuing {DocumentCount} documents for reprocessing", ids.Count);

            // Mark all Pending up front so a refresh immediately reflects the queued state.
            await _documentRepository.SetStatusAsync(ids, DocumentStatus.Pending, cancellationToken);

            foreach (var id in ids)
            {
                await _reprocessQueue.EnqueueAsync(new DocumentReprocessingJob(id), cancellationToken);
            }

            return new ReprocessResponse { DocumentsQueued = ids.Count };
        }

        // ---- Private helpers ----

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

        private async Task EnsureEmbeddingServiceAvailableAsync(CancellationToken cancellationToken)
        {
            if (!await _embeddingService.IsAvailableAsync(cancellationToken))
            {
                throw new InvalidOperationException("Embedding service not available. Ensure Ollama is running with the required model.");
            }
        }
    }
}
