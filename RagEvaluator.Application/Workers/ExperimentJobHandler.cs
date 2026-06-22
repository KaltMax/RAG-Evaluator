using Microsoft.Extensions.Logging;
using RagEvaluator.Application.Services.Interfaces;
using RagEvaluator.Contract.Abstractions.BackgroundProcessing;
using RagEvaluator.Contract.Dtos.Notifications;
using RagEvaluator.Domain.Enums;

namespace RagEvaluator.Application.Workers
{
    /// <summary>
    /// Runs an <see cref="ExperimentJob"/> by delegating to <see cref="IExperimentService.ProcessExperimentAsync"/>,
    /// which emits its own per-query progress and completion notifications. The handler adds the failure lifecycle:
    /// if processing throws it marks the experiment Failed and broadcasts a Failed notification.
    /// </summary>
    public class ExperimentJobHandler : IJobHandler<ExperimentJob>
    {
        private readonly IExperimentService _experimentService;
        private readonly IJobNotifier _jobNotifier;
        private readonly ILogger<ExperimentJobHandler> _logger;

        public ExperimentJobHandler(
            IExperimentService experimentService,
            IJobNotifier jobNotifier,
            ILogger<ExperimentJobHandler> logger)
        {
            _experimentService = experimentService;
            _jobNotifier = jobNotifier;
            _logger = logger;
        }

        public async Task HandleAsync(ExperimentJob job, CancellationToken cancellationToken)
        {
            try
            {
                await _experimentService.ProcessExperimentAsync(
                    job.ExperimentId, job.Queries, job.ResolvedDocumentIds, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process experiment {ExperimentId}", job.ExperimentId);

                await _experimentService.SetStatusAsync(job.ExperimentId, ExperimentStatus.Failed, CancellationToken.None);
                await NotifyFailedAsync(job.ExperimentId, cancellationToken);
            }
        }

        private async Task NotifyFailedAsync(Guid experimentId, CancellationToken cancellationToken)
        {
            var experiment = await _experimentService.GetByIdAsync(experimentId, CancellationToken.None);
            await _jobNotifier.NotifyAsync(
                new JobNotification(
                    JobTypes.Experiment,
                    experimentId,
                    ExperimentStatus.Failed.ToString(),
                    experiment?.Name,
                    experiment?.Progress.Completed,
                    experiment?.Progress.Total),
                cancellationToken);
        }
    }
}
