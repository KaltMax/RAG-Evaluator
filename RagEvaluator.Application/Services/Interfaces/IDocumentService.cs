using RagEvaluator.Contract.Dtos.Responses;
using RagEvaluator.Domain.Entities;
using RagEvaluator.Domain.ValueObjects;

namespace RagEvaluator.Application.Services.Interfaces
{
    /// <summary>
    /// Service for document and chunk operations including PDF processing, chunking, and embedding.
    /// </summary>
    public interface IDocumentService
    {
        Task<Document> CreateDocumentAsync(Stream fileStream, string fileName, long? fileSize, string? mimeType, string language);
        Task<DocumentResponse?> GetByIdAsync(Guid id);
        Task<IReadOnlyList<DocumentResponse>> GetAllAsync();
        Task<DocumentFileInfo?> GetDocumentFileInfoAsync(Guid id);
        Task UpdateStatusAsync(Guid id, DocumentStatus status, int? pageCount = null, int? chunkCount = null, string? content = null);
        Task DeleteAsync(Guid id);
        Task ProcessDocumentContentAsync(Guid documentId, Stream pdfStream);
        Task<IReadOnlyList<DocumentChunkResponse>> GetChunksByDocumentIdAsync(Guid documentId);
        Task<IReadOnlyList<ChunkSearchMatch>> SearchChunksAsync(float[] queryEmbedding, int topK);
    }
}
