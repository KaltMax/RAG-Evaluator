using RagEvaluator.Contract.Dtos.Responses;
using RagEvaluator.Domain.Entities;

namespace RagEvaluator.Application.Services.Interfaces
{
    public interface IDocumentService
    {
        Task<Document> CreateDocumentAsync(Stream fileStream, string fileName, long? fileSize, string? mimeType, string language);
        Task<DocumentResponse?> GetByIdAsync(Guid id);
        Task<IReadOnlyList<DocumentResponse>> GetAllAsync();
        Task<DocumentFileInfo?> GetDocumentFileInfoAsync(Guid id);
        Task UpdateStatusAsync(Guid id, DocumentStatus status, int? pageCount = null, int? chunkCount = null, string? content = null);
        Task DeleteAsync(Guid id);
    }
}
