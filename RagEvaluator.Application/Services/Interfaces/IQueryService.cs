using RagEvaluator.Contract.Dtos.Requests;
using RagEvaluator.Contract.Dtos.Responses;
using RagEvaluator.Domain.Entities;
using RagEvaluator.Domain.Enums;
using RagEvaluator.Domain.ValueObjects;

namespace RagEvaluator.Application.Services.Interfaces
{
    /// <summary>
    /// Service for query operations including creation, embedding generation, and persistence.
    /// </summary>
    public interface IQueryService
    {
        /// <summary>
        /// Checks if the query service is ready by verifying the embedding service availability.
        /// </summary>
        Task<bool> IsReadyAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a query by its unique identifier, including its results.
        /// </summary>
        Task<QueryResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all query summaries ordered by creation date.
        /// </summary>
        Task<IReadOnlyList<QuerySummaryResponse>> GetAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a new query entity and generates its embedding vector.
        /// </summary>
        Task<Query> CreateQueryAsync(string question, string language, int topK, string systemPrompt, string chunkingStrategy, string embeddingModel, string chatModel, CancellationToken cancellationToken = default);

        /// <summary>
        /// Completes a query by adding the answer, response time, and creating query results from chunk matches with similarity scores.
        /// </summary>
        Task CompleteQueryAsync(Query query, string answer, int responseTimeMs, IEnumerable<ChunkSearchMatch> chunkMatches, CancellationToken cancellationToken = default);

        /// <summary>
        /// Annotates query results with relevance grades, response quality evaluation, and ground truth relevant documents.
        /// </summary>
        Task AnnotateResultsAsync(Guid queryId, IEnumerable<QueryResultAnnotation> annotations, ResponseQuality responseQuality, bool hasLanguageSwitching, IEnumerable<Guid> relevantDocumentIds, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a query by its unique identifier.
        /// </summary>
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
