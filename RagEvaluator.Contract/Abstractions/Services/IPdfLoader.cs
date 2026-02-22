namespace RagEvaluator.Contract.Abstractions.Services
{
    /// <summary>
    /// Defines methods for loading PDF documents and retrieving page counts from file paths or streams.
    /// </summary>
    public interface IPdfLoader
    {
        /// <summary>
        /// Loads a PDF from a stream and extracts text from each page.
        /// </summary>
        List<string> LoadPdf(Stream stream);
    }
}
