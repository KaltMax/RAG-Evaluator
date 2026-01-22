namespace RagEvaluator.Contract.Abstractions.Services
{
    public interface IFileStorageService
    {
        Task<string> SaveFileAsync(Stream filestream, Guid documentId, string fileName);
        Task DeleteFileAsync(string filePath);
        bool FileExists(string filePath);
    }
}
