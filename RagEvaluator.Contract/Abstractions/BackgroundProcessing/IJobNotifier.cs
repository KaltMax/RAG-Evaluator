using RagEvaluator.Contract.Dtos.Notifications;

namespace RagEvaluator.Contract.Abstractions.BackgroundProcessing
{
    /// <summary>
    /// Notifies connected clients about the progress of background jobs.
    /// </summary>
    public interface IJobNotifier
    {
        Task NotifyAsync(JobNotification notification, CancellationToken cancellationToken = default);
    }
}
