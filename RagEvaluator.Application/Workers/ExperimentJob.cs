using RagEvaluator.Contract.Dtos.Requests;

namespace RagEvaluator.Application.Workers
{
    /// <summary>
    /// Background job payload for running an experiment.
    /// </summary>
    public sealed record ExperimentJob(
        Guid ExperimentId,
        List<ExperimentQueryItem> Queries,
        Dictionary<string, Guid> ResolvedDocumentIds);
}
