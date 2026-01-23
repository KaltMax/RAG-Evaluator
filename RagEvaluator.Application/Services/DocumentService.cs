using RagEvaluator.Application.Mappers;
using RagEvaluator.Application.Services.Interfaces;
using RagEvaluator.Contract.Abstractions.Data;
using RagEvaluator.Contract.Abstractions.Services;
using RagEvaluator.Contract.Dtos.Responses;
using RagEvaluator.Domain.Entities;

namespace RagEvaluator.Application.Services
{
    public class DocumentService : IDocumentService
    {
        private readonly IDocumentRepository _documentRepository;
        private readonly IFileStorageService _fileStorageService;

        public DocumentService(IDocumentRepository documentRepository, IFileStorageService fileStorageService)
        {
            _documentRepository = documentRepository;
            _fileStorageService = fileStorageService;
        }

        public async Task<Document> CreateDocumentAsync(Stream fileStream, string fileName, long? fileSize, string? mimeType, string language)
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
            var filePath = await _fileStorageService.SaveFileAsync(fileStream, document.Id, fileName);
            document.FilePath = filePath;

            await _documentRepository.AddAsync(document);
            return document;
        }

        public async Task<DocumentResponse?> GetByIdAsync(Guid id)
        {
            var document = await _documentRepository.GetByIdAsync(id);
            return document?.ToResponse();
        }

        public async Task<IReadOnlyList<DocumentResponse>> GetAllAsync()
        {
            var summaries = await _documentRepository.GetAllSummariesAsync();
            return summaries.ToResponseList();
        }

        public async Task<DocumentFileInfo?> GetDocumentFileInfoAsync(Guid id)
        {
            var document = await _documentRepository.GetByIdAsync(id);
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

        public async Task UpdateStatusAsync(Guid id, DocumentStatus status, int? pageCount = null, int? chunkCount = null, string? content = null)
        {
            var document = await _documentRepository.GetByIdAsync(id);
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

            await _documentRepository.UpdateAsync(document);
        }

        public async Task DeleteAsync(Guid id)
        {
            var document = await _documentRepository.GetByIdAsync(id);
            if (document?.FilePath != null)
            {
                await _fileStorageService.DeleteFileAsync(document.FilePath);
            }
            await _documentRepository.DeleteAsync(id);
        }
    }
}
