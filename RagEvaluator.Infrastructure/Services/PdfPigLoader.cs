using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using RagEvaluator.Contract.Abstractions.Services;

namespace RagEvaluator.Infrastructure.Services
{
    /// <summary>
    /// Service for loading and extracting text from PDF documents using ContentOrderTextExtractor.
    /// </summary>
    public class PdfPigLoader : IPdfLoader
    {
        /// <summary>
        /// Loads a PDF file and extracts text from each page
        /// </summary>
        /// <param name="pdfPath">Path to the PDF file</param>
        /// <returns>List of text content from each page</returns>
        /// <exception cref="FileNotFoundException">Thrown when PDF file doesn't exist</exception>
        public List<string> LoadPdf(string pdfPath)
        {
            if (!File.Exists(pdfPath))
            {
                throw new FileNotFoundException($"PDF file not found: {pdfPath}");
            }

            var pages = new List<string>();

            using var pdfDocument = PdfDocument.Open(pdfPath);

            foreach (var page in pdfDocument.GetPages())
            {
                var text = ContentOrderTextExtractor.GetText(page, addDoubleNewline: true);
                pages.Add(text);
            }

            return pages;
        }

        /// <summary>
        /// Loads a PDF from a stream and extracts text from each page
        /// </summary>
        /// <param name="stream">Stream containing PDF data</param>
        /// <returns>List of text content from each page</returns>
        public List<string> LoadPdf(Stream stream)
        {
            var pages = new List<string>();

            using var pdfDocument = PdfDocument.Open(stream);

            foreach (var page in pdfDocument.GetPages())
            {
                var text = ContentOrderTextExtractor.GetText(page, addDoubleNewline: true);
                pages.Add(text);
            }

            return pages;
        }

        /// <summary>
        /// Gets the number of pages in a PDF without loading all content
        /// </summary>
        public int GetPageCount(string pdfPath)
        {
            using var pdfDocument = PdfDocument.Open(pdfPath);
            return pdfDocument.NumberOfPages;
        }

        /// <summary>
        /// Gets the number of pages in a PDF from a stream
        /// </summary>
        public int GetPageCount(Stream stream)
        {
            using var pdfDocument = PdfDocument.Open(stream);
            return pdfDocument.NumberOfPages;
        }
    }
}
