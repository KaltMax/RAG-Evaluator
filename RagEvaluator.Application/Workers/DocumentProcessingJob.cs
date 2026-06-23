namespace RagEvaluator.Application.Workers
{
    /// <summary>
    /// Background job payload for processing an uploaded document.
    /// </summary>
    public sealed record DocumentProcessingJob(Guid DocumentId);
}
