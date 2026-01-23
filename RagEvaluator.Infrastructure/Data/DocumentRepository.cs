using Microsoft.EntityFrameworkCore;
using RagEvaluator.Contract.Abstractions.Data;
using RagEvaluator.Domain.Entities;

namespace RagEvaluator.Infrastructure.Data
{
    public class DocumentRepository : IDocumentRepository
    {
        private readonly ApplicationDbContext _context;

        public DocumentRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Document?> GetByIdAsync(Guid id)
        {
            return await _context.Documents.FindAsync(id);
        }

        public async Task<IReadOnlyList<Document>> GetAllAsync()
        {
            return await _context.Documents.ToListAsync();
        }

        public async Task<IReadOnlyList<DocumentSummary>> GetAllSummariesAsync()
        {
            return await _context.Documents
                .Select(d => new DocumentSummary
                {
                    Id = d.Id,
                    FileName = d.FileName,
                    FileSize = d.FileSize,
                    MimeType = d.MimeType,
                    Language = d.Language,
                    PageCount = d.PageCount,
                    ChunkCount = d.ChunkCount,
                    UploadedAt = d.UploadedAt,
                    ProcessedAt = d.ProcessedAt,
                    Status = d.Status
                })
                .ToListAsync();
        }

        public async Task<IReadOnlyList<Document>> GetByStatusAsync(DocumentStatus status)
        {
            return await _context.Documents
                .Where(d => d.Status == status)
                .ToListAsync();
        }

        public async Task AddAsync(Document document)
        {
            await _context.Documents.AddAsync(document);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Document document)
        {
            _context.Documents.Update(document);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var document = await _context.Documents.FindAsync(id);
            if (document is not null)
            {
                _context.Documents.Remove(document);
                await _context.SaveChangesAsync();
            }
        }
    }
}
