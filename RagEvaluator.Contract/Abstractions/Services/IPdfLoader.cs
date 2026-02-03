namespace RagEvaluator.Contract.Abstractions.Services
{
    /// <summary>
    /// Defines methods for loading PDF documents and retrieving page counts from file paths or streams.
    /// </summary>
    public interface IPdfLoader
    {
        /// <summary>
        /// Loads a PDF file and extracts text from each page.
        /// </summary>
        List<string> LoadPdf(string pdfPath);
        
        /// <summary>
        /// Loads a PDF from a stream and extracts text from each page.
        /// </summary>
        List<string> LoadPdf(Stream stream);
        
        /// <summary>
        /// Gets the number of pages in a PDF document from a file path.
        /// </summary>
        int GetPageCount(string pdfPath);
        
        /// <summary>
        /// Gets the number of pages in a PDF document from a stream.
        /// </summary>
        int GetPageCount(Stream stream);
    }
}
