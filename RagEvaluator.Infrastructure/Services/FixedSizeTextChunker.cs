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
        /// Splits a single text into chunks
        /// </summary>
        public Task<List<string>> CreateDocumentChunksAsync(string documentContent, CancellationToken cancellationToken = default)
        {
            var chunkSize = _config.ChunkSize;
            var chunkOverlap = _config.ChunkOverlap;

            if (chunkSize <= 0)
            {
                throw new InvalidOperationException("Chunk size must be positive.");
            }   
            if (chunkOverlap < 0)
            {
                throw new InvalidOperationException("Chunk overlap cannot be negative.");
            }
            if (chunkOverlap >= chunkSize)
            {
                throw new InvalidOperationException("Chunk overlap must be less than chunk size.");
            }

            var chunks = new List<string>();
            var startIndex = 0;

            while (startIndex < documentContent.Length)
            {
                var length = Math.Min(chunkSize, documentContent.Length - startIndex);
                var chunk = documentContent.Substring(startIndex, length);

                // Only add non-empty chunks
                if (!string.IsNullOrWhiteSpace(chunk))
                {
                    chunks.Add(chunk);
                }

                startIndex += chunkSize - chunkOverlap;
            }

            return Task.FromResult(chunks);
        }
    }
}
