namespace RagEvaluator.Application.Services.Interfaces
{
    public interface IChatService
    {
        Task<string> GenerateResponseAsync(string systemPrompt, string userMessage);
        Task<bool> IsAvailableAsync();
    }
}
