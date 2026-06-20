using RagEvaluator.Contract.Dtos.Requests;
using RagEvaluator.Contract.Dtos.Responses;
using RagEvaluator.Domain.Enums;
using RagEvaluator.Domain.ValueObjects;

namespace RagEvaluator.Application.Services.Interfaces
{
    /// <summary>
    /// Query feature service: runs the RAG question-answering pipeline and manages query
    /// history, annotation, and deletion.
    /// </summary>
    public interface IQueryService
    {
        /// <summary>
        /// Answers a question using RAG: searches for relevant document chunks and generates an answer using the LLM with retrieved context.
        /// </summary>
        Task<QueryResponse> AskQuestionAsync(AskQuestionRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a query by its unique identifier, including its results.
        /// </summary>
        Task<QueryResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all query summaries ordered by creation date.
        /// </summary>
        Task<IReadOnlyList<QuerySummaryResponse>> GetAllAsync(CancellationToken cancellationToken = default);

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
