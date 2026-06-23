using RagEvaluator.Domain.Entities;
using RagEvaluator.Domain.Enums;

namespace RagEvaluator.Contract.Abstractions.Data
{
    /// <summary>
    /// Defines the contract for an experiment repository, providing methods to manage experiments in the data store.
    /// </summary>
    public interface IExperimentRepository
    {
        /// <summary>
        /// Gets an experiment by its unique identifier, optionally including its associated queries and their results.
        /// </summary>
        Task<Experiment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets an experiment by its unique identifier with its associated queries and their results included.
        /// </summary>
        Task<Experiment?> GetByIdWithQueriesAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all experiments from the repository, ordered by creation date descending, including their associated queries.
        /// </summary>
        Task<IReadOnlyList<Experiment>> GetAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds a new experiment to the repository and saves changes to the data store.
        /// </summary>
        Task AddAsync(Experiment experiment, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing experiment in the repository and saves changes to the data store.
        /// </summary>
        Task UpdateAsync(Experiment experiment, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sets an experiment's status via a set-based update.
        /// </summary>
        Task SetStatusAsync(Guid id, ExperimentStatus status, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes an experiment by its unique identifier from the repository and saves changes to the data store.
        /// </summary>
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
