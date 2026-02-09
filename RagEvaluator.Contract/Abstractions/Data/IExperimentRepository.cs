using RagEvaluator.Domain.Entities;

namespace RagEvaluator.Contract.Abstractions.Data
{
    public interface IExperimentRepository
    {
        Task<Experiment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Experiment?> GetByIdWithQueriesAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Experiment>> GetAllAsync(CancellationToken cancellationToken = default);
        Task AddAsync(Experiment experiment, CancellationToken cancellationToken = default);
        Task UpdateAsync(Experiment experiment, CancellationToken cancellationToken = default);
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
