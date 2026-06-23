namespace RagEvaluator.Application.Workers
{
    /// <summary>
    /// Background job payload for reprocessing a document (re-chunk + re-embed from stored content).
    /// </summary>
    public sealed record DocumentReprocessingJob(Guid DocumentId);
}
