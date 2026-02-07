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
    /// Service for document and chunk operations including PDF processing, chunking, and embedding.
    /// </summary>
    public class DocumentService : IDocumentService
    {
        private readonly IDocumentRepository _documentRepository;
        private readonly IDocumentChunkRepository _documentChunkRepository;
        private readonly IFileStorageService _fileStorageService;
        private readonly IPdfLoader _pdfLoader;
        private readonly ITextChunker _textChunker;
        private readonly IEmbeddingService _embeddingService;
        private readonly RagConfiguration _config;

        public DocumentService(
            IDocumentRepository documentRepository,
            IDocumentChunkRepository documentChunkRepository,
            IFileStorageService fileStorageService,
            IPdfLoader pdfLoader,
            ITextChunker textChunker,
            IEmbeddingService embeddingService,
            RagConfiguration config)
        {
            _documentRepository = documentRepository;
            _documentChunkRepository = documentChunkRepository;
            _fileStorageService = fileStorageService;
            _pdfLoader = pdfLoader;
            _textChunker = textChunker;
            _embeddingService = embeddingService;
            _config = config;
        }

        public async Task<Document> CreateDocumentAsync(Stream fileStream, string fileName, long? fileSize, string? mimeType, string language, CancellationToken cancellationToken = default)
        {
            var document = new Document
            {
                Id = Guid.NewGuid(),
                FileName = fileName,
                FilePath = null,
                FileSize = fileSize,
                MimeType = mimeType,
                Language = language,
                UploadedAt = DateTime.UtcNow,
                Status = DocumentStatus.Pending
            };

            // Save file to storage
            var filePath = await _fileStorageService.SaveFileAsync(fileStream, document.Id, fileName, cancellationToken);
            document.FilePath = filePath;

            await _documentRepository.AddAsync(document, cancellationToken);
            return document;
        }

        public async Task<DocumentResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var document = await _documentRepository.GetByIdAsync(id, cancellationToken);
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

        public async Task UpdateStatusAsync(Guid id, DocumentStatus status, int? pageCount = null, int? chunkCount = null, string? content = null, CancellationToken cancellationToken = default)
        {
            var document = await _documentRepository.GetByIdAsync(id, cancellationToken);
            if (document is null)
            {
                throw new ArgumentException($"Document with id {id} not found");
            }

            document.Status = status;

            if (pageCount.HasValue)
            {
                document.PageCount = pageCount.Value;
            }

            if (chunkCount.HasValue)
            {
                document.ChunkCount = chunkCount.Value;
            }

            if (content is not null)
            {
                document.Content = content;
            }

            if (status == DocumentStatus.Completed)
            {
                document.ProcessedAt = DateTime.UtcNow;
            }

            await _documentRepository.UpdateAsync(document, cancellationToken);
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var document = await _documentRepository.GetByIdAsync(id, cancellationToken);

            if (document?.FilePath != null)
            {
                await _fileStorageService.DeleteFileAsync(document.FilePath);
            }
            await _documentChunkRepository.DeleteByDocumentIdAsync(id);
            await _documentRepository.DeleteAsync(id);
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

            // Split into chunks
            var textChunks = await _textChunker.CreateDocumentChunksAsync(content, cancellationToken);

            // Generate embeddings and create chunk entities
            var documentChunks = new List<DocumentChunk>();
            foreach (var chunkText in textChunks)
            {
                var embedding = await _embeddingService.GenerateEmbeddingAsync($"search_document: {chunkText}", cancellationToken);
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
            await UpdateStatusAsync(documentId, DocumentStatus.Completed, pages.Count, textChunks.Count, content);
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
