namespace RagEvaluator.Contract.Abstractions.Services
{
    /// <summary>
    /// Service for storing and managing uploaded document files.
    /// </summary>
    public interface IFileStorageService
    {
        Task<string> SaveFileAsync(Stream filestream, Guid documentId, string fileName);
        Task DeleteFileAsync(string filePath);
        bool FileExists(string filePath);
    }
}
