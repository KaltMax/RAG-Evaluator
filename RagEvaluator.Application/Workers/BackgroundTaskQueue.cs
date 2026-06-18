using System.Threading.Channels;
using RagEvaluator.Contract.Abstractions.BackgroundProcessing;

namespace RagEvaluator.Application.Workers
{
    /// <summary>
    /// In-memory <see cref="IBackgroundTaskQueue{TJob}"/> backed by an unbounded <see cref="Channel{T}"/>.
    /// Jobs are held only in memory, so any enqueued-but-unprocessed jobs are lost on restart.
    /// </summary>
    public class BackgroundTaskQueue<TJob> : IBackgroundTaskQueue<TJob>
    {
        private readonly Channel<TJob> _channel = Channel.CreateUnbounded<TJob>();

        public ValueTask EnqueueAsync(TJob job, CancellationToken cancellationToken = default)
        {
            return _channel.Writer.WriteAsync(job, cancellationToken);
        }

        public ValueTask<TJob> DequeueAsync(CancellationToken cancellationToken)
        {
            return _channel.Reader.ReadAsync(cancellationToken);
        }
    }
}
