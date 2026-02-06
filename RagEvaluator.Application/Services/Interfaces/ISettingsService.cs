using RagEvaluator.Contract.Dtos.Requests;
using RagEvaluator.Contract.Dtos.Responses;

namespace RagEvaluator.Application.Services.Interfaces
{
    /// <summary>
    /// Manages runtime RAG configuration: reading, validating, and applying settings changes.
    /// </summary>
    public interface ISettingsService
    {
        SettingsResponse GetSettings();
        Task<SettingsResponse> UpdateSettingsAsync(UpdateSettingsRequest request);
    }
}
