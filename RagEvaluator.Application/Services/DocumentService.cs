using RagEvaluator.Application.Mappers;
using RagEvaluator.Application.Services.Interfaces;
using RagEvaluator.Contract.Abstractions.Data;
using RagEvaluator.Contract.Abstractions.Services;
using RagEvaluator.Contract.Dtos.Responses;
using RagEvaluator.Domain.Entities;
using RagEvaluator.Domain.Enums;

namespace RagEvaluator.Application.Services
{
    /// <summary>
    /// Service for document CRUD operations.
    /// </summary>
    public class DocumentService : IDocumentService
    {
        private readonly IDocumentRepository _documentRepository;
        private readonly IDocumentChunkRepository _documentChunkRepository;
        private readonly IFileStorageService _fileStorageService;

        public DocumentService(
            IDocumentRepository documentRepository,
            IDocumentChunkRepository documentChunkRepository,
            IFileStorageService fileStorageService)
        {
            _documentRepository = documentRepository;
            _documentChunkRepository = documentChunkRepository;
            _fileStorageService = fileStorageService;
        }

        public async Task<Document> CreateDocumentAsync(Stream fileStream, string fileName, long fileSize, string mimeType, string language, string course, CancellationToken cancellationToken = default)
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
                FileSize = fileSize,
                MimeType = mimeType,
                Language = language,
                Course = course,
                UploadedAt = DateTime.UtcNow,
                Status = DocumentStatus.Pending
            };

            // Save file to storage
            var filePath = await _fileStorageService.SaveFileAsync(fileStream, document.Id, fileName, cancellationToken);
            document.FilePath = filePath;

            // Save document metadata to repository
            await _documentRepository.AddAsync(document, cancellationToken);
            return document;
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
                await _fileStorageService.DeleteFileAsync(document.FilePath, CancellationToken.None);
            }
            await _documentChunkRepository.DeleteByDocumentIdAsync(id, CancellationToken.None);
            await _documentRepository.DeleteAsync(id, CancellationToken.None);
        }
    }
}
