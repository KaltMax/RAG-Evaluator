using Microsoft.EntityFrameworkCore;
using RagEvaluator.Contract.Abstractions.Data;
using RagEvaluator.Domain.Entities;
using RagEvaluator.Domain.Enums;

namespace RagEvaluator.Infrastructure.Data.Repositories
{
    /// <summary>
    /// EF Core implementation of document repository using PostgreSQL.
    /// </summary>
    public class DocumentRepository : IDocumentRepository
    {
        private readonly ApplicationDbContext _context;

        public DocumentRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Documents.FindAsync([id], cancellationToken);
        }

        public async Task<Document?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            return await _context.Documents
                .FirstOrDefaultAsync(d => d.FileName == name, cancellationToken);
        }

        public async Task<IReadOnlyList<Document>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Documents.ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<DocumentSummary>> GetAllSummariesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Documents
                .Select(d => new DocumentSummary
                {
                    Id = d.Id,
                    FileName = d.FileName,
                    FileSize = d.FileSize,
                    MimeType = d.MimeType,
                    Language = d.Language,
                    Course = d.Course,
                    PageCount = d.PageCount,
                    ChunkCount = d.ChunkCount,
                    UploadedAt = d.UploadedAt,
                    ProcessedAt = d.ProcessedAt,
                    Status = d.Status
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Document>> GetByStatusAsync(DocumentStatus status, CancellationToken cancellationToken = default)
        {
            return await _context.Documents
                .Where(d => d.Status == status)
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(Document document, CancellationToken cancellationToken = default)
        {
            await _context.Documents.AddAsync(document, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(Document document, CancellationToken cancellationToken = default)
        {
            _context.Documents.Update(document);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task SetStatusAsync(IEnumerable<Guid> documentIds, DocumentStatus status, CancellationToken cancellationToken = default)
        {
            var ids = documentIds.ToList();
            await _context.Documents
                .Where(d => ids.Contains(d.Id))
                .ExecuteUpdateAsync(setters => setters.SetProperty(d => d.Status, status), cancellationToken);
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var document = await _context.Documents.FindAsync([id], cancellationToken);
            if (document is not null)
            {
                _context.Documents.Remove(document);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
