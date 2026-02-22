using RagEvaluator.Contract.Abstractions.Services;
using RagEvaluator.Contract.Configurations;

namespace RagEvaluator.Infrastructure.Services
{
    /// <summary>
    /// Splits text at topic boundaries detected via cosine similarity drops between consecutive line embeddings.
    /// </summary>
    public class SemanticTextChunker : ITextChunker
    {
        private readonly IEmbeddingService _embeddingService;
        private readonly RagConfiguration _config;

        public SemanticTextChunker(IEmbeddingService embeddingService, RagConfiguration config)
        {
            _embeddingService = embeddingService;
            _config = config;
        }

        public async Task<List<string>> CreateDocumentChunksAsync(string documentContent, CancellationToken cancellationToken = default)
        {
            var lines = documentContent.Split('\n')
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToList();

            if (lines.Count == 0)
            {
                return new List<string>();
            }
            if (lines.Count == 1)
            {
                return new List<string> { lines[0] };
            }
                
            // Embed each line
            var embeddings = new float[lines.Count][];
            for (var i = 0; i < lines.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                embeddings[i] = await _embeddingService.GenerateDocumentEmbeddingAsync(
                    lines[i], cancellationToken);
            }

            // Calculate cosine similarity between consecutive lines
            var similarities = new double[lines.Count - 1];
            for (var i = 0; i < lines.Count - 1; i++)
            {
                similarities[i] = CosineSimilarity(embeddings[i], embeddings[i + 1]);
            }

            // Build chunks by splitting at low-similarity boundaries
            var chunks = new List<string>();
            var currentLines = new List<string> {lines[0]};

            for (var i = 1; i < lines.Count; i++)
            {
                if (similarities[i - 1] < _config.SimilarityThreshold)
                {
                    chunks.Add(string.Join("\n", currentLines));
                    currentLines = new List<string>();
                }

                currentLines.Add(lines[i]);
            }

            // Add the last chunk
            if (currentLines.Count > 0)
            {
                chunks.Add(string.Join("\n", currentLines));
            }

            return chunks;
        }

        private static double CosineSimilarity(float[] a, float[] b)
        {
            double dotProduct = 0, magnitudeA = 0, magnitudeB = 0;

            for (var i = 0; i < a.Length; i++)
            {
                dotProduct += a[i] * (double)b[i];
                magnitudeA += a[i] * (double)a[i];
                magnitudeB += b[i] * (double)b[i];
            }

            var magnitude = Math.Sqrt(magnitudeA) * Math.Sqrt(magnitudeB);
            return magnitude == 0 ? 0 : dotProduct / magnitude;
        }
    }
}
