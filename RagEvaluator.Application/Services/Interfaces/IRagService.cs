using RagEvaluator.Contract.Dtos.Requests;
using RagEvaluator.Contract.Dtos.Responses;

namespace RagEvaluator.Application.Services.Interfaces
{
    /// <summary>
    /// Core service for RAG operations: document processing and question answering.
    /// </summary>
    public interface IRagService
    {
        /// <summary>
        /// Processes a document by creating it, extracting content, generating chunks with embeddings, and storing them.
        /// </summary>
        Task<DocumentResponse> ProcessDocumentAsync(Stream documentStream, string fileName, string contentType, string language, string course, CancellationToken cancellationToken = default);

        /// <summary>
        /// Answers a question using RAG: searches for relevant document chunks and generates an answer using the LLM with retrieved context.
        /// </summary>
        Task<QueryResponse> AskQuestionAsync(AskQuestionRequest request, CancellationToken cancellationToken = default);
    }
}
