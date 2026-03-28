using RagEvaluator.Domain.Entities;

namespace RagEvaluator.Contract.Abstractions.Data
{
    /// <summary>
    /// Repository for query persistence and retrieval.
    /// </summary>
    public interface IQueryRepository
    {
        /// <summary>
        /// Gets a query by its unique identifier.
        /// </summary>
        Task<Query?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a query by its unique identifier with its associated results included.
        /// </summary>
        Task<Query?> GetByIdWithResultsAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all queries from the repository.
        /// </summary>
        Task<IReadOnlyList<Query>> GetAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds a new query to the repository.
        /// </summary>
        Task AddAsync(Query query, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing query in the repository.
        /// </summary>
        Task UpdateAsync(Query query, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a query by its unique identifier.
        /// </summary>
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Finds unannotated sibling queries (same experiment, question, language, topK)
        /// for the given query, excluding the query itself.
        /// </summary>
        Task<IReadOnlyList<Query>> GetUnannotatedSiblingsAsync(Guid queryId, Guid experimentId, string question, string language, int topK, CancellationToken cancellationToken = default);
    }
}
