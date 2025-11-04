using RagEvaluator.Contract.Responses;

namespace RagEvaluator.Application.Services.Interfaces
{
    public interface IRagService
    {
        Task<DocumentResponse> ProcessDocumentAsync(Stream pdfStream, string fileName, string? description = null);
        Task<QueryResponse> AskQuestionAsync(string question, int topK = 3);
        Task<bool> IsInitializedAsync();
        Task<int> GetDocumentCountAsync();
    }
}
