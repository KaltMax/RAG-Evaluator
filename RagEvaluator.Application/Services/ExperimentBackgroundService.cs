using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RagEvaluator.Application.Services.Interfaces;

namespace RagEvaluator.Application.Services
{
    public class ExperimentBackgroundService : BackgroundService
    {
        private readonly ExperimentQueue _queue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ExperimentBackgroundService> _logger;

        public ExperimentBackgroundService(
            ExperimentQueue queue,
            IServiceScopeFactory scopeFactory,
            ILogger<ExperimentBackgroundService> logger)
        {
            _queue = queue;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ExperimentBackgroundService started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var (experimentId, queries) = await _queue.DequeueAsync(stoppingToken);
                    _logger.LogInformation("Processing experiment {ExperimentId}", experimentId);

                    using var scope = _scopeFactory.CreateScope();
                    var experimentService = scope.ServiceProvider.GetRequiredService<IExperimentService>();
                    await experimentService.ProcessExperimentAsync(experimentId, queries, stoppingToken);

                    _logger.LogInformation("Experiment {ExperimentId} completed", experimentId);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing experiment");
                }
            }

            _logger.LogInformation("ExperimentBackgroundService stopped");
        }
    }
}
