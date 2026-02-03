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

        public async Task<Query?> GetByIdAsync(Guid id)
        {
            return await _context.Queries.FindAsync(id);
        }

        public async Task<Query?> GetByIdWithResultsAsync(Guid id)
        {
            return await _context.Queries
                .Include(q => q.Results)
                .FirstOrDefaultAsync(q => q.Id == id);
        }

        public async Task<IReadOnlyList<Query>> GetAllAsync()
        {
            return await _context.Queries
                .OrderByDescending(q => q.CreatedAt)
                .ToListAsync();
        }

        public async Task AddAsync(Query query)
        {
            await _context.Queries.AddAsync(query);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Query query)
        {
            _context.Queries.Update(query);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var query = await _context.Queries.FindAsync(id);
            if (query is not null)
            {
                _context.Queries.Remove(query);
                await _context.SaveChangesAsync();
            }
        }
    }
}
