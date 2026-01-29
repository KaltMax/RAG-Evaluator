namespace RagEvaluator.Contract.Configurations
{
    /// <summary>
    /// Configuration settings for document file storage location.
    /// </summary>
    public class FileStorageConfiguration
    {
        public string BaseDirectory { get; set; } = "/app/uploads";
    }
}
