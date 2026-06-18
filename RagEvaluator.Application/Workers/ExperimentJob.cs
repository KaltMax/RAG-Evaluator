using RagEvaluator.Contract.Dtos.Requests;

namespace RagEvaluator.Application.Workers
{
    /// <summary>
    /// Background job payload for running an experiment: the experiment to run, its queries, and the
    /// pre-resolved document-name → id map used to populate ground-truth links.
    /// </summary>
    public sealed record ExperimentJob(
        Guid ExperimentId,
        List<ExperimentQueryItem> Queries,
        Dictionary<string, Guid> ResolvedDocumentIds);
}
