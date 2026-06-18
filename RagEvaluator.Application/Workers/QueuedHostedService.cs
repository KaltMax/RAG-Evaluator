using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RagEvaluator.Contract.Abstractions.BackgroundProcessing;

namespace RagEvaluator.Application.Workers
{
    /// <summary>
    /// Generic background worker that continuously drains an <see cref="IBackgroundTaskQueue{TJob}"/> and
    /// dispatches each job to a scoped <see cref="IJobHandler{TJob}"/>. A fresh DI scope is created per job so
    /// handlers can use scoped services. A failing job is logged and does not stop the worker.
    /// </summary>
    public class QueuedHostedService<TJob> : BackgroundService
    {
        private readonly IBackgroundTaskQueue<TJob> _queue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<QueuedHostedService<TJob>> _logger;
        private readonly string _jobName = typeof(TJob).Name;

        public QueuedHostedService(
            IBackgroundTaskQueue<TJob> queue,
            IServiceScopeFactory scopeFactory,
            ILogger<QueuedHostedService<TJob>> logger)
        {
            _queue = queue;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("{JobName} worker started", _jobName);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var job = await _queue.DequeueAsync(stoppingToken);
                    _logger.LogInformation("Processing {JobName}", _jobName);

                    using var scope = _scopeFactory.CreateScope();
                    var handler = scope.ServiceProvider.GetRequiredService<IJobHandler<TJob>>();
                    await handler.HandleAsync(job, stoppingToken);

                    _logger.LogInformation("{JobName} completed", _jobName);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing {JobName}", _jobName);
                }
            }

            _logger.LogInformation("{JobName} worker stopped", _jobName);
        }
    }
}
