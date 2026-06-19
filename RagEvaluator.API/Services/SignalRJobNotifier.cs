using Microsoft.AspNetCore.SignalR;
using RagEvaluator.API.Hubs;
using RagEvaluator.Contract.Abstractions.BackgroundProcessing;
using RagEvaluator.Contract.Dtos.Notifications;

namespace RagEvaluator.API.Services
{
    /// <summary>
    /// Broadcasts <see cref="JobNotification"/> updates to all connected clients via SignalR.
    /// Best-effort: delivery failures are logged and swallowed so they never disrupt the running job.
    /// </summary>
    public class SignalRJobNotifier : IJobNotifier
    {
        public const string JobUpdateMethod = "JobUpdate";
        private readonly IHubContext<JobsHub> _hubContext;
        private readonly ILogger<SignalRJobNotifier> _logger;

        public SignalRJobNotifier(IHubContext<JobsHub> hubContext, ILogger<SignalRJobNotifier> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task NotifyAsync(JobNotification notification, CancellationToken cancellationToken = default)
        {
            try
            {
                await _hubContext.Clients.All.SendAsync(JobUpdateMethod, notification, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to broadcast job notification for {JobType} {EntityId}",
                    notification.JobType, notification.EntityId);
            }
        }
    }
}
