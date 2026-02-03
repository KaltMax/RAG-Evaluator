using RagEvaluator.Domain.Entities;

namespace RagEvaluator.Contract.Abstractions.Data
{
    /// <summary>
    /// Repository for query persistence and retrieval.
    /// </summary>
    public interface IQueryRepository
    {
        Task<Query?> GetByIdAsync(Guid id);
        Task<Query?> GetByIdWithResultsAsync(Guid id);
        Task<IReadOnlyList<Query>> GetAllAsync();
        Task AddAsync(Query query);
        Task UpdateAsync(Query query);
        Task DeleteAsync(Guid id);
    }
}
