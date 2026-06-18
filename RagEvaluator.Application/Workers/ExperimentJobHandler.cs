using RagEvaluator.Application.Services.Interfaces;
using RagEvaluator.Contract.Abstractions.BackgroundProcessing;

namespace RagEvaluator.Application.Workers
{
    /// <summary>
    /// Dispatches an <see cref="ExperimentJob"/> to <see cref="IExperimentService.ProcessExperimentAsync"/>.
    /// </summary>
    public class ExperimentJobHandler : IJobHandler<ExperimentJob>
    {
        private readonly IExperimentService _experimentService;

        public ExperimentJobHandler(IExperimentService experimentService)
        {
            _experimentService = experimentService;
        }

        public Task HandleAsync(ExperimentJob job, CancellationToken cancellationToken)
        {
            return _experimentService.ProcessExperimentAsync(
                job.ExperimentId, job.Queries, job.ResolvedDocumentIds, cancellationToken);
        }
    }
}
