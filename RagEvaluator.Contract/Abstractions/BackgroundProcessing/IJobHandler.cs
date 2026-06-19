namespace RagEvaluator.Contract.Abstractions.BackgroundProcessing
{
    /// <summary>
    /// Processes a single background job of type <typeparamref name="TJob"/>. Resolved from a fresh DI scope
    /// per job by the hosted worker.
    /// </summary>
    public interface IJobHandler<TJob>
    {
        Task HandleAsync(TJob job, CancellationToken cancellationToken);
    }
}
