using RagEvaluator.Contract.Dtos.Requests;
using RagEvaluator.Contract.Dtos.Responses;

namespace RagEvaluator.Application.Services.Interfaces
{
    public interface IExperimentService
    {
        Task<ExperimentSummaryResponse> CreateExperimentAsync(CreateExperimentRequest request, CancellationToken cancellationToken = default);
        Task ProcessExperimentAsync(Guid experimentId, List<ExperimentQueryItem> queries, CancellationToken cancellationToken);
        Task<ExperimentResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ExperimentSummaryResponse>> GetAllAsync(CancellationToken cancellationToken = default);
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
