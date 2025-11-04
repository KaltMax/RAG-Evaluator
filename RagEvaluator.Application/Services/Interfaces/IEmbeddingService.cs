namespace RagEvaluator.Application.Services.Interfaces
{
    public interface IEmbeddingService
    {
        Task<float[]> GenerateEmbeddingAsync(string text);
        Task<bool> IsAvailableAsync();
    }
}
