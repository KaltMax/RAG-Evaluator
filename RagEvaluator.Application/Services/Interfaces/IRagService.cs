using RagEvaluator.Contract.Dtos.Requests;
using RagEvaluator.Contract.Dtos.Responses;

namespace RagEvaluator.Application.Services.Interfaces
{
    /// <summary>
    /// Core service for RAG operations: document processing and question answering.
    /// </summary>
    public interface IRagService
    {
        Task<DocumentResponse> ProcessDocumentAsync(Stream documentStream, string fileName, string contentType, string language);
        Task<QueryResponse> AskQuestionAsync(AskQuestionRequest request);
        Task<bool> IsInitializedAsync();
        Task<int> GetDocumentCountAsync();
    }
}
