using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using RagEvaluator.Contract.Abstractions.Services;

namespace RagEvaluator.Infrastructure.Services
{
    /// <summary>
    /// Service for loading and extracting text from PDF documents.
    /// Includes adaptive gap detection to preserve paragraph structure while removing
    /// headers and footers.
    /// </summary>
    public partial class PdfPigLoader : IPdfLoader
    {
        public List<string> LoadPdf(Stream stream)
        {
            var pages = new List<string>();
            using var pdfDocument = PdfDocument.Open(stream);

            foreach (var page in pdfDocument.GetPages())
            {
                var text = ExtractTextIgnoringBorders(page);
                pages.Add(CleanPageText(text));
            }

            return pages;
        }

        private static string ExtractTextIgnoringBorders(Page page)
        {
            // 1. Geometric Filtering: Get words only within the central content area
            var contentWords = GetContentWords(page);
            if (contentWords.Count == 0) return string.Empty;

            // 2. Structural Analysis: Group words into lines and calculate adaptive spacing
            var groupedLines = contentWords
                .GroupBy(w => Math.Round(w.BoundingBox.Bottom))
                .OrderByDescending(g => g.Key)
                .ToList();

            var adaptiveThreshold = CalculateAdaptiveThreshold(contentWords);

            // 3. Text Reconstruction: Join lines using gap detection for paragraphs
            return ReconstructTextWithGapDetection(groupedLines, adaptiveThreshold);
        }

        private static List<Word> GetContentWords(Page page)
        {
            var pageHeight = page.Height;

            // Thresholds to remove institutional headers (logos) and footers (page numbers/copyright)
            var footerThreshold = pageHeight * 0.07;
            var headerThreshold = pageHeight * 0.95;

            return page.GetWords()
                .Where(w => w.BoundingBox.Bottom >= footerThreshold &&
                            w.BoundingBox.Top <= headerThreshold)
                .ToList();
        }

        private static double CalculateAdaptiveThreshold(List<Word> words)
        {
            // Multiplier of 2.2 differentiates between standard line spacing and paragraph breaks
            var avgHeight = words.Average(w => w.BoundingBox.Height);
            return avgHeight * 2.2;
        }

        private static string ReconstructTextWithGapDetection(IEnumerable<IGrouping<double, Word>> groupedLines, double threshold)
        {
            var sb = new System.Text.StringBuilder();
            double? lastY = null;

            foreach (var lineGroup in groupedLines)
            {
                var currentY = lineGroup.Key;
                var lineText = string.Join(" ", lineGroup.OrderBy(w => w.BoundingBox.Left).Select(w => w.Text));

                if (lastY.HasValue)
                {
                    double gap = lastY.Value - currentY;

                    // If the vertical drop exceeds the adaptive threshold, insert a double newline
                    sb.Append(gap > threshold ? "\n\n" : "\n");
                }

                sb.Append(lineText);
                lastY = currentY;
            }

            return sb.ToString();
        }

        private static string CleanPageText(string pageText)
        {
            if (string.IsNullOrWhiteSpace(pageText)) return string.Empty;

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
