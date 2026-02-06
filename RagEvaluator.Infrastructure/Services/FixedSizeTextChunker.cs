using RagEvaluator.Contract.Abstractions.Services;
using RagEvaluator.Contract.Configurations;

namespace RagEvaluator.Infrastructure.Services
{
    /// <summary>
    /// Service for splitting text into manageable chunks with overlap
    /// </summary>
    public class FixedSizeTextChunker : ITextChunker
    {
        private readonly int _chunkSize;
        private readonly int _chunkOverlap;

        /// <summary>
        /// Creates a new TextChunker with specified configuration
        /// </summary>
        /// <param name="config">RAG configuration containing chunk size and overlap settings</param>
        public FixedSizeTextChunker(RagConfiguration config)
        {
            if (config.ChunkSize <= 0)
            {
                throw new ArgumentException("Chunk size must be positive", nameof(config.ChunkSize));
            }

            if (config.ChunkOverlap < 0)
            {
                throw new ArgumentException("Chunk overlap cannot be negative", nameof(config.ChunkOverlap));
            }

            if (config.ChunkOverlap >= config.ChunkSize)
            {
                throw new ArgumentException("Chunk overlap must be less than chunk size", nameof(config.ChunkOverlap));
            }

            _chunkSize = config.ChunkSize;
            _chunkOverlap = config.ChunkOverlap;
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
            var chunks = new List<string>();
            var startIndex = 0;

            while (startIndex < text.Length)
            {
                var length = Math.Min(_chunkSize, text.Length - startIndex);
                var chunk = text.Substring(startIndex, length);

                // Only add non-empty chunks
                if (!string.IsNullOrWhiteSpace(chunk))
                {
                    chunks.Add(chunk);
                }

                startIndex += _chunkSize - _chunkOverlap;

                // Prevent infinite loop if overlap >= chunk size
                if (startIndex <= 0)
                    startIndex = _chunkSize;
            }

            return chunks;
        }
    }
}
