using RagEvaluator.Application.Services.Interfaces;
using RagEvaluator.Contract.Abstractions.Data;
using RagEvaluator.Domain.Entities;

namespace RagEvaluator.Application.Services
{
    public class DocumentService : IDocumentService
    {
        private readonly IDocumentRepository _documentRepository;

        public DocumentService(IDocumentRepository documentRepository)
        {
            _documentRepository = documentRepository;
        }

        public async Task<Document> CreateDocumentAsync(string fileName, string? filePath, long? fileSize, string? mimeType)
        {
            var document = new Document
            {
                Id = Guid.NewGuid(),
                FileName = fileName,
                FilePath = filePath,
                FileSize = fileSize,
                MimeType = mimeType,
                UploadedAt = DateTime.UtcNow,
                Status = DocumentStatus.Pending
            };

            await _documentRepository.AddAsync(document);
            return document;
        }

        public async Task<Document?> GetByIdAsync(Guid id)
        {
            return await _documentRepository.GetByIdAsync(id);
        }

        public async Task<IReadOnlyList<Document>> GetAllAsync()
        {
            return await _documentRepository.GetAllAsync();
        }

        public async Task UpdateStatusAsync(Guid id, DocumentStatus status, int? pageCount = null, int? chunkCount = null)
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

            if (status == DocumentStatus.Completed)
            {
                document.ProcessedAt = DateTime.UtcNow;
            }

            await _documentRepository.UpdateAsync(document);
        }

        public async Task DeleteAsync(Guid id)
        {
            await _documentRepository.DeleteAsync(id);
        }
    }
}
