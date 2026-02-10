using RagEvaluator.Contract.Abstractions.Services;
using RagEvaluator.Contract.Configurations;

namespace RagEvaluator.Infrastructure.Services
{
    /// <summary>
    /// File storage implementation using the local file system.
    /// </summary>
    public class LocalFileStorageService : IFileStorageService
    {
        private readonly string _baseDirectory;

        public LocalFileStorageService(FileStorageConfiguration config)
        {
            _baseDirectory = config.BaseDirectory;
            Directory.CreateDirectory(_baseDirectory);
        }

        public async Task<string> SaveFileAsync(Stream fileStream, Guid documentId, string fileName, CancellationToken cancellationToken = default)
        {
            var extension = Path.GetExtension(fileName);
            var storedFileName = $"{documentId}{extension}";
            var filePath = Path.Combine(_baseDirectory, storedFileName);

            using var file = File.Create(filePath);
            fileStream.Position = 0;
            await fileStream.CopyToAsync(file, cancellationToken);

            return filePath;
        }

        public Task DeleteFileAsync(string filePath, CancellationToken cancellationToken = default)
        {
            if(File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            return Task.CompletedTask;
        }

        public bool FileExists(string filePath)
        {
            return File.Exists(filePath);
        }
    }
}
