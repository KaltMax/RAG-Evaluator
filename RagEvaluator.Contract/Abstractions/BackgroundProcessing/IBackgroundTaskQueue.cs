namespace RagEvaluator.Contract.Abstractions.BackgroundProcessing
{
    /// <summary>
    /// A queue of background jobs of type <typeparamref name="TJob"/>. Producers enqueue jobs from a
    /// request scope; a single hosted worker drains them on a background thread. Registered as a singleton
    /// so the producer and the worker share the same underlying queue.
    /// </summary>
    public interface IBackgroundTaskQueue<TJob>
    {
        ValueTask EnqueueAsync(TJob job, CancellationToken cancellationToken = default);

        ValueTask<TJob> DequeueAsync(CancellationToken cancellationToken);
    }
}
