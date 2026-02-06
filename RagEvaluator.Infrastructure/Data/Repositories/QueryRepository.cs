using Microsoft.EntityFrameworkCore;
using RagEvaluator.Contract.Abstractions.Data;
using RagEvaluator.Domain.Entities;

namespace RagEvaluator.Infrastructure.Data.Repositories
{
    /// <summary>
    /// EF Core implementation of query repository using PostgreSQL.
    /// </summary>
    public class QueryRepository : IQueryRepository
    {
        private readonly ApplicationDbContext _context;

        public QueryRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Query?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Queries.FindAsync([id], cancellationToken);
        }

        public async Task<Query?> GetByIdWithResultsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Queries
                .Include(q => q.Results)
                .Include(q => q.RelevantDocuments)
                .FirstOrDefaultAsync(q => q.Id == id, cancellationToken);
        }

        public async Task<IReadOnlyList<Query>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Queries
                .OrderByDescending(q => q.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(Query query, CancellationToken cancellationToken = default)
        {
            await _context.Queries.AddAsync(query, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(Query query, CancellationToken cancellationToken = default)
        {
            _context.Queries.Update(query);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var query = await _context.Queries.FindAsync([id], cancellationToken);
            if (query is not null)
            {
                _context.Queries.Remove(query);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
