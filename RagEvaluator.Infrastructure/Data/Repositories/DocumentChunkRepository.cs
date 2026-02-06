using System.Globalization;
using Microsoft.EntityFrameworkCore;
using RagEvaluator.Contract.Abstractions.Data;
using RagEvaluator.Domain.Entities;
using RagEvaluator.Domain.ValueObjects;

namespace RagEvaluator.Infrastructure.Data.Repositories
{
    /// <summary>
    /// EF Core implementation of chunk repository with pgvector similarity search.
    /// </summary>
    public class DocumentChunkRepository : IDocumentChunkRepository
    {
        private readonly ApplicationDbContext _context;

        public DocumentChunkRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<DocumentChunk?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.DocumentChunks.FindAsync([id], cancellationToken);
        }

        public async Task<IReadOnlyList<DocumentChunk>> GetByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default)
        {
            return await _context.DocumentChunks
                .Where(c => c.DocumentId == documentId)
                .ToListAsync(cancellationToken);
        }

        public async Task<int> GetCountAsync(CancellationToken cancellationToken = default)
        {
            return await _context.DocumentChunks.CountAsync(cancellationToken);
        }

        public async Task AddAsync(DocumentChunk chunk, CancellationToken cancellationToken = default)
        {
            await _context.DocumentChunks.AddAsync(chunk, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task AddRangeAsync(IEnumerable<DocumentChunk> chunks, CancellationToken cancellationToken = default)
        {
            await _context.DocumentChunks.AddRangeAsync(chunks, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default)
        {
            var chunks = await _context.DocumentChunks
                .Where(c => c.DocumentId == documentId)
                .ToListAsync(cancellationToken);

            if (chunks.Count > 0)
            {
                _context.DocumentChunks.RemoveRange(chunks);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task<IReadOnlyList<ChunkSearchMatch>> SearchAsync(float[] queryEmbedding, int topK = 3, CancellationToken cancellationToken = default)
        {
            // Convert float[] to pgvector format string: [0.1,0.2,0.3,...]
            var vectorString = "[" + string.Join(",", queryEmbedding.Select(v => v.ToString(CultureInfo.InvariantCulture))) + "]";

            // Use raw SQL with pgvector's cosine distance operator (<=>)
            // The query returns chunks ordered by similarity (closest first)
            var chunks = await _context.DocumentChunks
                .FromSqlRaw(
                    """
                    SELECT "Id", "Text", "Embedding", "ChunkingStrategy", "EmbeddingModel", "DocumentId"
                    FROM "DocumentChunks"
                    ORDER BY "Embedding" <=> {0}::vector
                    LIMIT {1}
                    """,
                    vectorString, topK)
                .ToListAsync(cancellationToken);

            // Get document file names for the results
            var documentIds = chunks.Select(c => c.DocumentId).Distinct().ToList();
            var documents = await _context.Documents
                .Where(d => documentIds.Contains(d.Id))
                .ToDictionaryAsync(d => d.Id, d => d.FileName, cancellationToken);

            // Map to ChunkSearchMatch
            return chunks.Select(c => new ChunkSearchMatch
            {
                Id = c.Id,
                Text = c.Text,
                Embedding = c.Embedding,
                DocumentId = c.DocumentId,
                FileName = documents.GetValueOrDefault(c.DocumentId, string.Empty),
                ChunkingStrategy = c.ChunkingStrategy,
                EmbeddingModel = c.EmbeddingModel
            }).ToList();
        }
    }
}
