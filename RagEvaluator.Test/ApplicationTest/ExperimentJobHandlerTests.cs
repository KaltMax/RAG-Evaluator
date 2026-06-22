using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using RagEvaluator.Application.Services.Interfaces;
using RagEvaluator.Application.Workers;
using RagEvaluator.Contract.Abstractions.BackgroundProcessing;
using RagEvaluator.Contract.Dtos.Notifications;
using RagEvaluator.Contract.Dtos.Requests;
using RagEvaluator.Contract.Dtos.Responses;
using RagEvaluator.Domain.Enums;

namespace RagEvaluator.Test.ApplicationTest
{
    public class ExperimentJobHandlerTests
    {
        private readonly IExperimentService _experimentService;
        private readonly IJobNotifier _jobNotifier;
        private readonly ILogger<ExperimentJobHandler> _logger;
        private readonly ExperimentJobHandler _handler;

        public ExperimentJobHandlerTests()
        {
            _experimentService = Substitute.For<IExperimentService>();
            _jobNotifier = Substitute.For<IJobNotifier>();
            _logger = Substitute.For<ILogger<ExperimentJobHandler>>();
            _handler = new ExperimentJobHandler(_experimentService, _jobNotifier, _logger);
        }

        [Fact]
        public async Task HandleAsync_OnSuccess_ProcessesWithoutMarkingFailed()
        {
            // Arrange
            var job = CreateJob();

            // Act
            await _handler.HandleAsync(job, TestContext.Current.CancellationToken);

            // Assert — processing runs; the service owns its own progress/completion notifications
            await _experimentService.Received(1).ProcessExperimentAsync(
                job.ExperimentId, job.Queries, job.ResolvedDocumentIds, Arg.Any<CancellationToken>());
            await _experimentService.DidNotReceive().SetStatusAsync(
                Arg.Any<Guid>(), Arg.Any<ExperimentStatus>(), Arg.Any<CancellationToken>());
            await _jobNotifier.DidNotReceive().NotifyAsync(Arg.Any<JobNotification>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task HandleAsync_WhenProcessingThrows_MarksFailedAndNotifies()
        {
            // Arrange
            var job = CreateJob();
            _experimentService.ProcessExperimentAsync(
                    job.ExperimentId, Arg.Any<List<ExperimentQueryItem>>(), Arg.Any<Dictionary<string, Guid>>(), Arg.Any<CancellationToken>())
                .ThrowsAsync(new InvalidOperationException("processing failed"));
            _experimentService.GetByIdAsync(job.ExperimentId, Arg.Any<CancellationToken>())
                .Returns(CreateResponse(job.ExperimentId));

            // Act
            await _handler.HandleAsync(job, TestContext.Current.CancellationToken);

            // Assert — failure persists via a set-based update and is broadcast
            await _experimentService.Received(1).SetStatusAsync(
                job.ExperimentId, ExperimentStatus.Failed, Arg.Any<CancellationToken>());
            await _jobNotifier.Received(1).NotifyAsync(
                Arg.Is<JobNotification>(n => n.JobType == JobTypes.Experiment && n.Status == ExperimentStatus.Failed.ToString()),
                Arg.Any<CancellationToken>());
        }

        private static ExperimentJob CreateJob()
        {
            return new ExperimentJob(Guid.NewGuid(), [], []);
        }

        private static ExperimentResponse CreateResponse(Guid id)
        {
            return new ExperimentResponse
            {
                Id = id,
                Name = "Test Experiment",
                Status = ExperimentStatus.Failed.ToString(),
                EmbeddingModel = "nomic-embed-text-v2-moe",
                ChunkingStrategy = "FixedSize",
                ChatModel = "qwen2.5:14b",
                PromptTemplate = "Basic",
                Progress = new ExperimentProgress { Total = 4, Completed = 2 }
            };
        }
    }
}
