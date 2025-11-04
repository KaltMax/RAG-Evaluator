namespace RagEvaluator.Application.Services.Interfaces
{
    public interface IPdfLoader
    {
        List<string> LoadPdf(string pdfPath);
        List<string> LoadPdf(Stream stream);
        int GetPageCount(string pdfPath);
        int GetPageCount(Stream stream);
    }
}
