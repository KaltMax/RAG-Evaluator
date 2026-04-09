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

        /// <summary>
        /// Creates a new SemanticTextChunker with the specified embedding service and configuration.
        /// </summary>
        /// <param name="embeddingService"></param>
        /// <param name="config"></param>
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

            var embeddings = await EmbedLinesAsync(lines, cancellationToken);
            var similarities = CalculateConsecutiveSimilarities(embeddings);
            return BuildChunks(lines, similarities);
        }

        private async Task<float[][]> EmbedLinesAsync(List<string> lines, CancellationToken cancellationToken)
        {
            var embeddings = new float[lines.Count][];
            for (var i = 0; i < lines.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                embeddings[i] = await _embeddingService.GenerateDocumentEmbeddingAsync(
                    lines[i], cancellationToken);
            }
            return embeddings;
        }

        private double[] CalculateConsecutiveSimilarities(float[][] embeddings)
        {
            var similarities = new double[embeddings.Length - 1];
            for (var i = 0; i < similarities.Length; i++)
            {
                similarities[i] = CosineSimilarity(embeddings[i], embeddings[i + 1]);
            }
            return similarities;
        }

        private List<string> BuildChunks(List<string> lines, double[] similarities)
        {
            var cutoff = CalculatePercentileCutoff(similarities);
            var chunks = new List<string>();
            var currentLines = new List<string> { lines[0] };
            var currentLength = lines[0].Length;

            for (var i = 1; i < lines.Count; i++)
            {
                var candidateLength = currentLength + 1 + lines[i].Length;

                if ((similarities[i - 1] < cutoff && currentLength >= _config.MinChunkSize) || candidateLength > _config.ChunkSize)
                {
                    chunks.Add(string.Join("\n", currentLines));
                    currentLines = new List<string>();
                    currentLength = 0;
                }

                currentLines.Add(lines[i]);
                if (currentLength > 0)
                {
                    currentLength = currentLength + 1 + lines[i].Length;
                }
                else
                {
                    currentLength = lines[i].Length;
                }
            }

            if (currentLines.Count > 0)
            {
                chunks.Add(string.Join("\n", currentLines));
            }

            return chunks;
        }

        private double CalculatePercentileCutoff(double[] similarities)
        {
            var sorted = similarities.OrderBy(s => s).ToArray();
            var index = (int)(sorted.Length * _config.SimilarityThreshold);
            return sorted[Math.Min(index, sorted.Length - 1)];
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
            if (magnitude == 0)
            {
                return 0;
            }

            return dotProduct / magnitude;
        }
    }
}
