using RagEvaluator.Contract.Dtos.Responses;

namespace RagEvaluator.Application.Services.Interfaces
{
    /// <summary>
    /// Core service for RAG operations: document processing and question answering.
    /// </summary>
    public interface IRagService
    {
        Task<DocumentResponse> ProcessDocumentAsync(Stream pdfStream, string fileName, string language);
        Task<QueryResponse> AskQuestionAsync(string question, int topK = 3);
        Task<bool> IsInitializedAsync();
        Task<int> GetDocumentCountAsync();
    }
}
