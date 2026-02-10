using RagEvaluator.Contract.Dtos.Responses;
using RagEvaluator.Domain.Entities;

namespace RagEvaluator.Application.Mappers
{
    /// <summary>
    /// Extension methods for mapping Document entities to DTOs.
    /// </summary>
    public static class DocumentMapper
    {
        public static DocumentResponse ToResponse(this Document document)
        {
            return new DocumentResponse
            {
                Id = document.Id,
                FileName = document.FileName,
                FileSize = document.FileSize,
                MimeType = document.MimeType,
                Language = document.Language,
                PageCount = document.PageCount,
                ChunkCount = document.ChunkCount,
                UploadedAt = document.UploadedAt,
                ProcessedAt = document.ProcessedAt,
                Status = document.Status.ToString()
            };
        }

        public static DocumentResponse ToResponse(this DocumentSummary summary)
        {
            return new DocumentResponse
            {
                Id = summary.Id,
                FileName = summary.FileName,
                FileSize = summary.FileSize,
                MimeType = summary.MimeType,
                Language = summary.Language,
                PageCount = summary.PageCount,
                ChunkCount = summary.ChunkCount,
                UploadedAt = summary.UploadedAt,
                ProcessedAt = summary.ProcessedAt,
                Status = summary.Status.ToString()
            };
        }

        public static DocumentChunkResponse ToResponse(this DocumentChunk chunk)
        {
            return new DocumentChunkResponse
            {
                Id = chunk.Id,
                Text = chunk.Text,
                ChunkingStrategy = chunk.ChunkingStrategy,
                EmbeddingModel = chunk.EmbeddingModel,
                DocumentId = chunk.DocumentId
            };
        }

        public static IReadOnlyList<DocumentResponse> ToResponseList(this IEnumerable<Document> documents)
        {
            return documents.Select(d => d.ToResponse()).ToList();
        }

        public static IReadOnlyList<DocumentResponse> ToResponseList(this IEnumerable<DocumentSummary> summaries)
        {
            return summaries.Select(s => s.ToResponse()).ToList();
        }

        public static IReadOnlyList<DocumentChunkResponse> ToResponseList(this IEnumerable<DocumentChunk> chunks)
        {
            return chunks.Select(c => c.ToResponse()).ToList();
        }
    }
}
