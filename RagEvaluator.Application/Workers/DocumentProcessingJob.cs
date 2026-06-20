namespace RagEvaluator.Application.Workers
{
    /// <summary>
    /// Background job payload for processing an uploaded document. Carries only the id; the file is
    /// reloaded from storage by the worker.
    /// </summary>
    public sealed record DocumentProcessingJob(Guid DocumentId);
}
