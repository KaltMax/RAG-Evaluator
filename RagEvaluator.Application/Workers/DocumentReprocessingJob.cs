namespace RagEvaluator.Application.Workers
{
    /// <summary>
    /// Background job payload for reprocessing a document (re-chunk + re-embed from stored content).
    /// Carries only the id; the content is reloaded by the worker.
    /// </summary>
    public sealed record DocumentReprocessingJob(Guid DocumentId);
}
