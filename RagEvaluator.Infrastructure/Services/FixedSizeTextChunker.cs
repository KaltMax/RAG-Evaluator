using RagEvaluator.Contract.Abstractions.Services;
using RagEvaluator.Contract.Configurations;

namespace RagEvaluator.Infrastructure.Services
{
    /// <summary>
    /// Service for splitting text into manageable chunks with overlap
    /// </summary>
    public class FixedSizeTextChunker : ITextChunker
    {
        private readonly RagConfiguration _config;

        /// <summary>
        /// Creates a new TextChunker with specified configuration
        /// </summary>
        /// <param name="config">RAG configuration containing chunk size and overlap settings</param>
        public FixedSizeTextChunker(RagConfiguration config)
        {
            _config = config;
        }

        /// <summary>
        /// Splits a list of documents into chunks
        /// </summary>
        public Task<List<string>> SplitDocumentsAsync(List<string> documents, CancellationToken cancellationToken = default)
        {
            var chunks = new List<string>();

            foreach (var doc in documents)
            {
                chunks.AddRange(SplitText(doc));
            }

            return Task.FromResult(chunks);
        }

        /// <summary>
        /// Splits a single text into chunks
        /// </summary>
        public Task<List<string>> SplitTextAsync(string text, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(SplitText(text));
        }

        private List<string> SplitText(string text)
        {
            var chunkSize = _config.ChunkSize;
            var chunkOverlap = _config.ChunkOverlap;

            if (chunkSize <= 0)
                throw new InvalidOperationException("Chunk size must be positive.");
            if (chunkOverlap < 0)
                throw new InvalidOperationException("Chunk overlap cannot be negative.");
            if (chunkOverlap >= chunkSize)
                throw new InvalidOperationException("Chunk overlap must be less than chunk size.");

            var chunks = new List<string>();
            var startIndex = 0;

            while (startIndex < text.Length)
            {
                var length = Math.Min(chunkSize, text.Length - startIndex);
                var chunk = text.Substring(startIndex, length);

                // Only add non-empty chunks
                if (!string.IsNullOrWhiteSpace(chunk))
                {
                    chunks.Add(chunk);
                }

                startIndex += chunkSize - chunkOverlap;

                // Prevent infinite loop if overlap >= chunk size
                if (startIndex <= 0)
                    startIndex = chunkSize;
            }

            return chunks;
        }
    }
}
