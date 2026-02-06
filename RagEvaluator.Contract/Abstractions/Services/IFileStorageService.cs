namespace RagEvaluator.Contract.Abstractions.Services
{
    /// <summary>
    /// Service for storing and managing uploaded document files.
    /// </summary>
    public interface IFileStorageService
    {
        /// <summary>
        /// Saves a file stream to storage and returns the file path.
        /// </summary>
        Task<string> SaveFileAsync(Stream filestream, Guid documentId, string fileName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a file from storage by its file path.
        /// </summary>
        Task DeleteFileAsync(string filePath, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Checks whether a file exists at the specified file path.
        /// </summary>
        bool FileExists(string filePath);
    }
}
