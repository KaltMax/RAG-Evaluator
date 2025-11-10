namespace RagEvaluator.Contract.Abstractions.Services
{
    /// <summary>
    /// Defines methods for interacting with a chat-based AI service, including generating responses and checking
    /// service availability.
    /// </summary>
    public interface IChatService
    {
        Task<string> GenerateResponseAsync(string systemPrompt, string userMessage);
        Task<bool> IsAvailableAsync();
    }
}
