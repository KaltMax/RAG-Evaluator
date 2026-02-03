using RagEvaluator.Contract.Dtos.Requests;
using RagEvaluator.Contract.Dtos.Responses;
using RagEvaluator.Domain.Entities;
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
        Task<bool> IsReadyAsync();
        
        /// <summary>
        /// Gets a query summary by its unique identifier.
        /// </summary>
        Task<QuerySummaryResponse?> GetByIdAsync(Guid id);
        
        /// <summary>
        /// Gets all query summaries ordered by creation date.
        /// </summary>
        Task<IReadOnlyList<QuerySummaryResponse>> GetAllAsync();
        
        /// <summary>
        /// Creates a new query entity and generates its embedding vector.
        /// </summary>
        Task<Query> CreateQueryAsync(string question, string language, int topK, string systemPrompt, string chunkingStrategy, string embeddingModel, string chatModel);
        
        /// <summary>
        /// Completes a query by adding the answer, response time, and creating query results from chunk matches with similarity scores.
        /// </summary>
        Task CompleteQueryAsync(Query query, string answer, int responseTimeMs, IEnumerable<ChunkSearchMatch> chunkMatches);
        
        /// <summary>
        /// Annotates query results with relevance grades for evaluation purposes.
        /// </summary>
        Task AnnotateResultsAsync(Guid queryId, IEnumerable<ResultAnnotation> annotations);
        
        /// <summary>
        /// Calculates retrieval metrics (MRR, Precision@K, Recall@K, NDCG@K) for a query based on relevance annotations.
        /// </summary>
        Task CalculateMetricsAsync(Guid queryId);
        
        /// <summary>
        /// Deletes a query by its unique identifier.
        /// </summary>
        Task DeleteAsync(Guid id);
    }
}
