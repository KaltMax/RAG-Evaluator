using System.Globalization;
using Microsoft.EntityFrameworkCore;
using RagEvaluator.Contract.Abstractions.Data;
using RagEvaluator.Domain.Entities;
using RagEvaluator.Domain.ValueObjects;

namespace RagEvaluator.Infrastructure.Data.Repositories
{
    public class DocumentChunkRepository : IDocumentChunkRepository
    {
        private readonly ApplicationDbContext _context;

        public DocumentChunkRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<DocumentChunk?> GetByIdAsync(Guid id)
        {
            return await _context.DocumentChunks.FindAsync(id);
        }

        public async Task<IReadOnlyList<DocumentChunk>> GetByDocumentIdAsync(Guid documentId)
        {
            return await _context.DocumentChunks
                .Where(c => c.DocumentId == documentId)
                .ToListAsync();
        }

        public async Task<int> GetCountAsync()
        {
            return await _context.DocumentChunks.CountAsync();
        }

        public async Task AddAsync(DocumentChunk chunk)
        {
            await _context.DocumentChunks.AddAsync(chunk);
            await _context.SaveChangesAsync();
        }

        public async Task AddRangeAsync(IEnumerable<DocumentChunk> chunks)
        {
            await _context.DocumentChunks.AddRangeAsync(chunks);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteByDocumentIdAsync(Guid documentId)
        {
            var chunks = await _context.DocumentChunks
                .Where(c => c.DocumentId == documentId)
                .ToListAsync();

            if (chunks.Count > 0)
            {
                _context.DocumentChunks.RemoveRange(chunks);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IReadOnlyList<SearchResult>> SearchAsync(float[] queryEmbedding, int topK = 3)
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
                .ToListAsync();

            // Get document file names for the results
            var documentIds = chunks.Select(c => c.DocumentId).Distinct().ToList();
            var documents = await _context.Documents
                .Where(d => documentIds.Contains(d.Id))
                .ToDictionaryAsync(d => d.Id, d => d.FileName);

            // Calculate similarity scores (1 - cosine distance) and map to SearchResult
            return chunks.Select(c => new SearchResult
            {
                Id = c.Id,
                Text = c.Text,
                Similarity = 1 - CosineDistance(queryEmbedding, c.Embedding),
                DocumentId = c.DocumentId,
                FileName = documents.GetValueOrDefault(c.DocumentId, string.Empty),
                ChunkingStrategy = c.ChunkingStrategy,
                EmbeddingModel = c.EmbeddingModel
            }).ToList();
        }

        private static double CosineDistance(float[] a, float[] b)
        {
            if (a.Length != b.Length)
            {
                return 1.0;
            }

            double dotProduct = 0;
            double magnitudeA = 0;
            double magnitudeB = 0;

            for (int i = 0; i < a.Length; i++)
            {
                dotProduct += a[i] * b[i];
                magnitudeA += a[i] * a[i];
                magnitudeB += b[i] * b[i];
            }

            magnitudeA = Math.Sqrt(magnitudeA);
            magnitudeB = Math.Sqrt(magnitudeB);

            if (magnitudeA == 0 || magnitudeB == 0)
            {
                return 1.0;
            }

            var similarity = dotProduct / (magnitudeA * magnitudeB);
            return 1 - similarity;
        }
    }
}
