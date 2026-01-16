using RagEvaluator.Contract.Dtos.Responses;

namespace RagEvaluator.Application.Services.Interfaces
{
    public interface IRagService
    {
        Task<DocumentResponse> ProcessDocumentAsync(Stream pdfStream, string fileName);
        Task<QueryResponse> AskQuestionAsync(string question, int topK = 3);
        Task<bool> IsInitializedAsync();
        Task<int> GetDocumentCountAsync();
    }
}
