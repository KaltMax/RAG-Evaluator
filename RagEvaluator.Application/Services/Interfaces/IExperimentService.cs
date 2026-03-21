using RagEvaluator.Contract.Dtos.Requests;
using RagEvaluator.Contract.Dtos.Responses;

namespace RagEvaluator.Application.Services.Interfaces
{
    /// <summary>
    /// Defines the contract for the experiment service, providing methods to create, process, retrieve, and delete experiments.
    /// </summary>
    public interface IExperimentService
    {
        /// <summary>
        /// Creates a new experiment with a configuration snapshot and enqueues it for background processing.
        /// </summary>
        Task<ExperimentSummaryResponse> CreateExperimentAsync(CreateExperimentRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Processes an experiment by executing its queries sequentially and linking the results to the experiment.
        /// </summary>
        Task ProcessExperimentAsync(Guid experimentId, List<ExperimentQueryItem> queries, Dictionary<string, Guid> resolvedDocumentIds, CancellationToken cancellationToken);

        /// <summary>
        /// Gets an experiment by its unique identifier, including aggregated metrics computed from its linked queries.
        /// </summary>
        Task<ExperimentResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all experiments as summary responses, ordered by creation date descending.
        /// </summary>
        Task<IReadOnlyList<ExperimentSummaryResponse>> GetAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes an experiment by its unique identifier.
        /// </summary>
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
