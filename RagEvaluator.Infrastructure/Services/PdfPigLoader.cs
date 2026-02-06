using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using RagEvaluator.Contract.Abstractions.Services;

namespace RagEvaluator.Infrastructure.Services
{
    /// <summary>
    /// Service for loading and extracting text from PDF documents using ContentOrderTextExtractor.
    /// Includes post-processing to remove bare URLs and normalize whitespace.
    /// </summary>
    public partial class PdfPigLoader : IPdfLoader
    {
        /// <summary>
        /// Loads a PDF file and extracts text from each page
        /// </summary>
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
                pages.Add(CleanPageText(text));
            }

            return pages;
        }

        /// <summary>
        /// Loads a PDF from a stream and extracts text from each page
        /// </summary>
        public List<string> LoadPdf(Stream stream)
        {
            var pages = new List<string>();
            using var pdfDocument = PdfDocument.Open(stream);

            foreach (var page in pdfDocument.GetPages())
            {
                var text = ContentOrderTextExtractor.GetText(page, addDoubleNewline: true);
                pages.Add(CleanPageText(text));
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

        private static string CleanPageText(string pageText)
        {
            var lines = pageText.Split('\n');
            var cleanedLines = new List<string>();

            foreach (var line in lines)
            {
                // Remove lines that are only a URL (image sources, attribution links)
                if (IsBareUrl(line.Trim())) continue;

                cleanedLines.Add(line);
            }

            var result = string.Join('\n', cleanedLines);

            // Collapse 3+ consecutive newlines into 2 (single blank line)
            result = MultipleNewlinesRegex().Replace(result, "\n\n");

            return result.Trim();
        }

        private static bool IsBareUrl(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return false;
            return BareUrlRegex().IsMatch(line);
        }

        [GeneratedRegex(@"\n{3,}")]
        private static partial Regex MultipleNewlinesRegex();

        [GeneratedRegex(@"^https?://\S+$")]
        private static partial Regex BareUrlRegex();
    }
}
