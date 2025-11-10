namespace RagEvaluator.Contract.Abstractions.Services
{
    /// <summary>
    /// Defines methods for loading PDF documents and retrieving page counts from file paths or streams.
    /// </summary>
    public interface IPdfLoader
    {
        List<string> LoadPdf(string pdfPath);
        List<string> LoadPdf(Stream stream);
        int GetPageCount(string pdfPath);
        int GetPageCount(Stream stream);
    }
}
