using RagEvaluator.Domain.Entities;

namespace RagEvaluator.Application.Services.Interfaces
{
    public interface IDocumentService
    {
        Task<Document> CreateDocumentAsync(Stream fileStream, string fileName, long? fileSize, string? mimeType);
        Task<Document?> GetByIdAsync(Guid id);
        Task<IReadOnlyList<Document>> GetAllAsync();
        Task UpdateStatusAsync(Guid id, DocumentStatus status, int? pageCount = null, int? chunkCount = null);
        Task DeleteAsync(Guid id);
    }
}
