using Microsoft.EntityFrameworkCore;
using RagEvaluator.Contract.Abstractions.Data;
using RagEvaluator.Domain.Entities;

namespace RagEvaluator.Infrastructure.Data.Repositories
{
    /// <summary>
    /// EF Core implementation of experiment repository using PostgreSQL.
    /// </summary>
    public class ExperimentRepository : IExperimentRepository
    {
        private readonly ApplicationDbContext _context;

        public ExperimentRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Experiment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Experiments.FindAsync([id], cancellationToken);
        }

        public async Task<Experiment?> GetByIdWithQueriesAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Experiments
                .Include(e => e.Queries)
                    .ThenInclude(q => q.Results)
                .Include(e => e.Queries)
                    .ThenInclude(q => q.RelevantDocuments)
                .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        }

        public async Task<IReadOnlyList<Experiment>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Experiments
                .Include(e => e.Queries)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(Experiment experiment, CancellationToken cancellationToken = default)
        {
            await _context.Experiments.AddAsync(experiment, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(Experiment experiment, CancellationToken cancellationToken = default)
        {
            _context.Experiments.Update(experiment);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var experiment = await _context.Experiments.FindAsync([id], cancellationToken);
            if (experiment is not null)
            {
                _context.Experiments.Remove(experiment);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
