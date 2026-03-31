using Microsoft.Extensions.Logging;
using RagEvaluator.Application.Mappers;
using RagEvaluator.Application.Services.Interfaces;
using RagEvaluator.Application.Workers;
using RagEvaluator.Contract.Abstractions.Data;
using RagEvaluator.Contract.Configurations;
using RagEvaluator.Contract.Dtos.Requests;
using RagEvaluator.Contract.Dtos.Responses;
using RagEvaluator.Domain.Entities;
using RagEvaluator.Domain.Enums;

namespace RagEvaluator.Application.Services
{
    /// <summary>
    /// Service for managing experiments, including creation, processing, retrieval, and deletion.
    /// </summary>
    public class ExperimentService : IExperimentService
    {
        private readonly ILogger<ExperimentService> _logger;
        private readonly IExperimentRepository _experimentRepository;
        private readonly IQueryRepository _queryRepository;
        private readonly IDocumentRepository _documentRepository;
        private readonly IRagService _ragService;
        private readonly ExperimentQueue _experimentQueue;
        private readonly RagConfiguration _config;

        public ExperimentService(
            ILogger<ExperimentService> logger,
            IExperimentRepository experimentRepository,
            IQueryRepository queryRepository,
            IDocumentRepository documentRepository,
            IRagService ragService,
            ExperimentQueue experimentQueue,
            RagConfiguration config)
        {
            _logger = logger;
            _experimentRepository = experimentRepository;
            _queryRepository = queryRepository;
            _documentRepository = documentRepository;
            _ragService = ragService;
            _experimentQueue = experimentQueue;
            _config = config;
        }

        public async Task<ExperimentSummaryResponse> CreateExperimentAsync(
            CreateExperimentRequest request, CancellationToken cancellationToken = default)
        {
            // Resolve document names to IDs
            var resolvedDocumentIds = new Dictionary<string, Guid>();
            var allDocumentNames = request.Queries
                .SelectMany(q => q.RelevantDocumentNames)
                .Distinct()
                .ToList();

            foreach (var name in allDocumentNames)
            {
                var document = await _documentRepository.GetByNameAsync(name, cancellationToken);
                if (document is null)
                {
                    throw new ArgumentException($"Unknown document name in RelevantDocumentNames: {name}");
                }
                resolvedDocumentIds[name] = document.Id;
            }

            var experiment = new Experiment
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                RepeatCount = request.RepeatCount,
                Status = ExperimentStatus.Running,
                TotalQueryCount = request.Queries.Count * request.RepeatCount,
                CompletedQueryCount = 0,
                EmbeddingModel = _config.EmbeddingModel,
                ChunkingStrategy = _config.ChunkingStrategy.ToString(),
                ChatModel = _config.ChatModel,
                ChunkSize = _config.ChunkSize,
                ChunkOverlap = _config.ChunkOverlap,
                SimilarityThreshold = _config.SimilarityThreshold,
                MinChunkSize = _config.MinChunkSize,
                PromptTemplate = _config.PromptTemplate.ToString()
            };

            await _experimentRepository.AddAsync(experiment, cancellationToken);
            await _experimentQueue.EnqueueAsync(experiment.Id, request.Queries, resolvedDocumentIds, cancellationToken);

            return experiment.ToSummary();
        }

        public async Task ProcessExperimentAsync(Guid experimentId, List<ExperimentQueryItem> queries, Dictionary<string, Guid> resolvedDocumentIds, CancellationToken cancellationToken)
        {
            var experiment = await _experimentRepository.GetByIdAsync(experimentId, cancellationToken);
            if (experiment is null)
            {
                _logger.LogError("Experiment {ExperimentId} not found for processing", experimentId);
                return;
            }

            for (var repeat = 0; repeat < experiment.RepeatCount; repeat++)
            {
                foreach (var queryItem in queries)
                {
                    if (cancellationToken.IsCancellationRequested)
                        return;

                    try
                    {
                        await ProcessSingleQueryAsync(experiment, queryItem, resolvedDocumentIds, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Experiment {ExperimentId}: failed to process query '{Question}' (repeat {Repeat})",
                            experiment.Id, queryItem.Question, repeat + 1);
                    }
                }
            }

            experiment.Status = ExperimentStatus.Completed;
            experiment.CompletedAt = DateTime.UtcNow;
            await _experimentRepository.UpdateAsync(experiment, cancellationToken);
        }

        private async Task ProcessSingleQueryAsync(Experiment experiment, ExperimentQueryItem queryItem, Dictionary<string, Guid> resolvedDocumentIds, CancellationToken cancellationToken)
        {
            var askRequest = new AskQuestionRequest
            {
                Question = queryItem.Question,
                Language = queryItem.Language,
                TopK = queryItem.TopK
            };

            var response = await _ragService.AskQuestionAsync(askRequest, cancellationToken);

            var query = await _queryRepository.GetByIdWithResultsAsync(response.QueryId, cancellationToken);
            if (query is not null)
            {
                query.ExperimentId = experiment.Id;

                foreach (var name in queryItem.RelevantDocumentNames)
                {
                    if (resolvedDocumentIds.TryGetValue(name, out var documentId))
                    {
                        query.RelevantDocuments.Add(new QueryRelevantDocument
                        {
                            QueryId = query.Id,
                            DocumentId = documentId
                        });
                    }
                }

                await _queryRepository.UpdateAsync(query, cancellationToken);
            }

            experiment.CompletedQueryCount++;
            await _experimentRepository.UpdateAsync(experiment, cancellationToken);

            _logger.LogInformation(
                "Experiment {ExperimentId}: completed query {Completed}/{Total}",
                experiment.Id, experiment.CompletedQueryCount, experiment.TotalQueryCount);
        }

        public async Task<ExperimentResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var experiment = await _experimentRepository.GetByIdWithQueriesAsync(id, cancellationToken);
            return experiment?.ToResponse();
        }

        public async Task<IReadOnlyList<ExperimentSummaryResponse>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var experiments = await _experimentRepository.GetAllAsync(cancellationToken);
            return experiments.Select(e => e.ToSummary()).ToList();
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            await _experimentRepository.DeleteAsync(id, cancellationToken);
        }
    }
}
