using RagEvaluator.Contract.Configurations;
using RagEvaluator.Domain.Enums;
using RagEvaluator.Infrastructure.Services;

namespace RagEvaluator.Test.InfrastructureTest
{
    public class FixedSizeTextChunkerTests
    {
        #region CreateDocumentChunksAsync Tests

        [Fact]
        public async Task CreateDocumentChunksAsync_ShouldSplitTextIntoCorrectNumberOfChunks()
        {
            // Arrange: 10 chars, chunk size 5, no overlap -> 2 chunks
            var chunker = CreateChunker(chunkSize: 5, chunkOverlap: 0);

            // Act
            var result = await chunker.CreateDocumentChunksAsync("AAAAABBBBB", TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal("AAAAA", result[0]);
            Assert.Equal("BBBBB", result[1]);
        }

        [Fact]
        public async Task CreateDocumentChunksAsync_WithOverlap_ShouldCreateOverlappingChunks()
        {
            // Arrange: 8 chars, chunk size 6, overlap 2 -> step=4
            var chunker = CreateChunker(chunkSize: 6, chunkOverlap: 2);
            var text = "ABCDEFGH"; // 8 chars, step=4

            // Act
            var result = await chunker.CreateDocumentChunksAsync(text, TestContext.Current.CancellationToken);

            // Assert: start=0 "ABCDEF", start=4 "EFGH" (overlap on "EF")
            Assert.Equal(2, result.Count);
            Assert.Equal("ABCDEF", result[0]);
            Assert.Equal("EFGH", result[1]);
        }

        [Fact]
        public async Task CreateDocumentChunksAsync_WithLastChunkSmaller_ShouldIncludeRemainder()
        {
            // Arrange: 7 chars, chunk size 5, no overlap -> "AAAAA" + "BB"
            var chunker = CreateChunker(chunkSize: 5, chunkOverlap: 0);

            // Act
            var result = await chunker.CreateDocumentChunksAsync("AAAAABB", TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal("AAAAA", result[0]);
            Assert.Equal("BB", result[1]);
        }

        [Fact]
        public async Task CreateDocumentChunksAsync_WithTextSmallerThanChunkSize_ShouldReturnSingleChunk()
        {
            // Arrange
            var chunker = CreateChunker(chunkSize: 1000, chunkOverlap: 200);

            // Act
            var result = await chunker.CreateDocumentChunksAsync("Short text", TestContext.Current.CancellationToken);

            // Assert
            Assert.Single(result);
            Assert.Equal("Short text", result[0]);
        }

        [Fact]
        public async Task CreateDocumentChunksAsync_WithEmptyText_ShouldReturnEmptyList()
        {
            // Arrange
            var chunker = CreateChunker(chunkSize: 100, chunkOverlap: 0);

            // Act
            var result = await chunker.CreateDocumentChunksAsync("", TestContext.Current.CancellationToken);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task CreateDocumentChunksAsync_WithWhitespaceOnlyText_ShouldReturnEmptyList()
        {
            // Arrange
            var chunker = CreateChunker(chunkSize: 5, chunkOverlap: 0);

            // Act
            var result = await chunker.CreateDocumentChunksAsync("     ", TestContext.Current.CancellationToken);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task CreateDocumentChunksAsync_WithZeroChunkSize_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var chunker = CreateChunker(chunkSize: 0, chunkOverlap: 0);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                chunker.CreateDocumentChunksAsync("Some text", TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task CreateDocumentChunksAsync_WithNegativeOverlap_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var chunker = CreateChunker(chunkSize: 100, chunkOverlap: -1);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                chunker.CreateDocumentChunksAsync("Some text", TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task CreateDocumentChunksAsync_WithOverlapEqualToChunkSize_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var chunker = CreateChunker(chunkSize: 100, chunkOverlap: 100);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                chunker.CreateDocumentChunksAsync("Some text", TestContext.Current.CancellationToken));
        }

        #endregion

        #region Helper Methods

        private FixedSizeTextChunker CreateChunker(int chunkSize, int chunkOverlap)
        {
            var config = new RagConfiguration
            {
                ChunkSize = chunkSize,
                ChunkOverlap = chunkOverlap,
                OllamaEndpoint = "http://localhost:11434/v1",
                EmbeddingModel = "nomic-embed-text-v2-moe",
                AvailableEmbeddingModels = "nomic-embed-text-v2-moe",
                ChatModel = "qwen2.5:14b",
                ChunkingStrategy = ChunkingStrategy.FixedSize,
                PromptTemplate = PromptTemplate.Basic,
                PromptBasic = "",
                PromptInstructed = "",
                PromptLanguageAwareEn = "",
                PromptLanguageAwareDe = "",
                SimilarityThreshold = 0.5
            };
            return new FixedSizeTextChunker(config);
        }

        #endregion
    }
}
