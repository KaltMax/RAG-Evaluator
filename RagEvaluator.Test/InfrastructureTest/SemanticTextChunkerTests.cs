using NSubstitute;
using RagEvaluator.Contract.Abstractions.Services;
using RagEvaluator.Contract.Configurations;
using RagEvaluator.Domain.Enums;
using RagEvaluator.Infrastructure.Services;

namespace RagEvaluator.Test.InfrastructureTest
{
    public class SemanticTextChunkerTests
    {
        private readonly IEmbeddingService _embeddingService;

        public SemanticTextChunkerTests()
        {
            _embeddingService = Substitute.For<IEmbeddingService>();
        }

        #region CreateDocumentChunksAsync Tests

        [Fact]
        public async Task CreateDocumentChunksAsync_WithSimilarLines_ShouldGroupIntoSingleChunk()
        {
            // Arrange: all similarities identical -> nothing falls below percentile cutoff
            var chunker = CreateChunker(similarityThreshold: 0.5);
            var text = "Line one\nLine two\nLine three";

            // Return similar embeddings for all lines
            _embeddingService.GenerateDocumentEmbeddingAsync(Arg.Any<string>(), TestContext.Current.CancellationToken)
                .Returns(new float[] { 1.0f, 0.0f, 0.0f });

            // Act
            var result = await chunker.CreateDocumentChunksAsync(text, TestContext.Current.CancellationToken);

            // Assert: all lines similar, single chunk
            Assert.Single(result);
            Assert.Equal("Line one\nLine two\nLine three", result[0]);
        }

        [Fact]
        public async Task CreateDocumentChunksAsync_WithDissimilarLines_ShouldSplitAtBoundary()
        {
            // Arrange: similarities [1.0, 0.0] → 0.0 falls below percentile cutoff → split
            var chunker = CreateChunker(similarityThreshold: 0.5);
            var text = "Topic A first\nTopic A second\nTopic B first";

            var similarEmbedding = new float[] { 1.0f, 0.0f, 0.0f };
            var differentEmbedding = new float[] { 0.0f, 1.0f, 0.0f }; // orthogonal → similarity 0

            _embeddingService.GenerateDocumentEmbeddingAsync("Topic A first", TestContext.Current.CancellationToken)
                .Returns(similarEmbedding);
            _embeddingService.GenerateDocumentEmbeddingAsync("Topic A second", TestContext.Current.CancellationToken)
                .Returns(similarEmbedding);
            _embeddingService.GenerateDocumentEmbeddingAsync("Topic B first", TestContext.Current.CancellationToken)
                .Returns(differentEmbedding);

            // Act
            var result = await chunker.CreateDocumentChunksAsync(text, TestContext.Current.CancellationToken);

            // Assert: split between "Topic A second" and "Topic B first"
            Assert.Equal(2, result.Count);
            Assert.Equal("Topic A first\nTopic A second", result[0]);
            Assert.Equal("Topic B first", result[1]);
        }

        [Fact]
        public async Task CreateDocumentChunksAsync_WithEmptyText_ShouldReturnEmptyList()
        {
            // Arrange
            var chunker = CreateChunker(similarityThreshold: 0.5);

            // Act
            var result = await chunker.CreateDocumentChunksAsync("", TestContext.Current.CancellationToken);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task CreateDocumentChunksAsync_WithSingleLine_ShouldReturnSingleChunk()
        {
            // Arrange
            var chunker = CreateChunker(similarityThreshold: 0.5);

            // Act
            var result = await chunker.CreateDocumentChunksAsync("Only one line", TestContext.Current.CancellationToken);

            // Assert
            Assert.Single(result);
            Assert.Equal("Only one line", result[0]);
        }

        [Fact]
        public async Task CreateDocumentChunksAsync_ShouldSkipBlankLines()
        {
            // Arrange
            var chunker = CreateChunker(similarityThreshold: 0.5);
            var text = "Line one\n\n\nLine two";

            _embeddingService.GenerateDocumentEmbeddingAsync(Arg.Any<string>(), TestContext.Current.CancellationToken)
                .Returns(new float[] { 1.0f, 0.0f });

            // Act
            var result = await chunker.CreateDocumentChunksAsync(text, TestContext.Current.CancellationToken);

            // Assert: blank lines filtered, 2 non-blank lines embedded
            await _embeddingService.Received(2).GenerateDocumentEmbeddingAsync(Arg.Any<string>(), TestContext.Current.CancellationToken);
            Assert.Single(result);
        }

        [Fact]
        public async Task CreateDocumentChunksAsync_ShouldPassRawLinesForEmbedding()
        {
            // Arrange: need at least 2 lines (single line hits early return without embedding)
            var chunker = CreateChunker(similarityThreshold: 0.5);

            _embeddingService.GenerateDocumentEmbeddingAsync(Arg.Any<string>(), TestContext.Current.CancellationToken)
                .Returns(new float[] { 1.0f });

            // Act
            await chunker.CreateDocumentChunksAsync("Hello world\nSecond line", TestContext.Current.CancellationToken);

            // Assert
            await _embeddingService.Received(1).GenerateDocumentEmbeddingAsync(
                "Hello world", TestContext.Current.CancellationToken);
            await _embeddingService.Received(1).GenerateDocumentEmbeddingAsync(
                "Second line", TestContext.Current.CancellationToken);
        }

        [Fact]
        public async Task CreateDocumentChunksAsync_WithWhitespaceOnlyLines_ShouldReturnEmptyList()
        {
            // Arrange
            var chunker = CreateChunker(similarityThreshold: 0.5);

            // Act
            var result = await chunker.CreateDocumentChunksAsync("   \n  \n   ", TestContext.Current.CancellationToken);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task CreateDocumentChunksAsync_WithSimilarLines_ShouldSplitWhenExceedingChunkSize()
        {
            // Arrange: all lines are highly similar, but chunk size is small enough to force splits
            var chunker = CreateChunker(similarityThreshold: 0.5, chunkSize: 30);
            var text = "Line one here\nLine two here\nLine three here";

            // Return identical embeddings → similarity = 1.0, well above threshold
            _embeddingService.GenerateDocumentEmbeddingAsync(Arg.Any<string>(), TestContext.Current.CancellationToken)
                .Returns(new float[] { 1.0f, 0.0f, 0.0f });

            // Act
            var result = await chunker.CreateDocumentChunksAsync(text, TestContext.Current.CancellationToken);

            // Assert: despite high similarity, chunks are split because joining would exceed chunkSize
            Assert.True(result.Count > 1, $"Expected multiple chunks but got {result.Count}");
            Assert.All(result, chunk => Assert.True(chunk.Length <= 30, $"Chunk exceeds max size: \"{chunk}\" ({chunk.Length} chars)"));
        }

        #endregion

        #region Helper Methods

        private SemanticTextChunker CreateChunker(double similarityThreshold, int chunkSize = 1000)
        {
            var config = new RagConfiguration
            {
                SimilarityThreshold = similarityThreshold,
                OllamaEndpoint = "http://localhost:11434/v1",
                EmbeddingModel = "nomic-embed-text-v2-moe",
                AvailableEmbeddingModels = "nomic-embed-text-v2-moe",
                ChatModel = "qwen2.5:14b",
                ChunkingStrategy = ChunkingStrategy.Semantic,
                PromptTemplate = PromptTemplate.Basic,
                PromptBasic = "",
                PromptInstructed = "",
                PromptLanguageAwareEn = "",
                PromptLanguageAwareDe = "",
                ChunkSize = chunkSize,
                MinChunkSize = 0
            };
            return new SemanticTextChunker(_embeddingService, config);
        }

        #endregion
    }
}
