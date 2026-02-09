using System.Threading.Channels;
using RagEvaluator.Contract.Dtos.Requests;

namespace RagEvaluator.Application.Workers
{
    /// <summary>
    /// In-memory queue for managing experiment processing. 
    /// Uses a Channel to enqueue experiment IDs and their associated queries for background processing.
    /// </summary>
    public class ExperimentQueue
    {
        private readonly Channel<(Guid ExperimentId, List<ExperimentQueryItem> Queries)> _channel =
            Channel.CreateUnbounded<(Guid, List<ExperimentQueryItem>)>();

        public async ValueTask EnqueueAsync(Guid experimentId, List<ExperimentQueryItem> queries, CancellationToken cancellationToken = default)
        {
            await _channel.Writer.WriteAsync((experimentId, queries), cancellationToken);
        }

        public async ValueTask<(Guid ExperimentId, List<ExperimentQueryItem> Queries)> DequeueAsync(CancellationToken cancellationToken)
        {
            return await _channel.Reader.ReadAsync(cancellationToken);
        }
    }
}
