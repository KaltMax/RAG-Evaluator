namespace RagEvaluator.Contract.Abstractions.Services
{
    /// <summary>
    /// Defines methods for interacting with a chat-based AI service, including generating responses and checking
    /// service availability.
    /// </summary>
    public interface IChatService
    {
        /// <summary>
        /// Generates a response from the AI service based on a system prompt and user message.
        /// </summary>
        Task<string> GenerateResponseAsync(string systemPrompt, string userMessage);
        
        /// <summary>
        /// Checks whether the chat service is available and ready to accept requests.
        /// </summary>
        Task<bool> IsAvailableAsync();
    }
}
