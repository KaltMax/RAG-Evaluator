using RagEvaluator.Application.Services.Interfaces;
using RagEvaluator.Application.Services;
using RagEvaluator.Domain.Entities;

namespace RagEvaluator.Test.ApplicationTest
{
    public class MetricsServiceTests
    {
        private IMetricsService _service;

        public MetricsServiceTests()
        {
            _service = new MetricsService();
        }

        #region CosineSimilarityTests

        [Fact]
        public void CosineSimilarity_IdenticalVectors_ReturnsOne()
        {
            // Arrange
            var vec1 = new float[] { 1, 0, 0 };
            var vec2 = new float[] { 1, 0, 0 };

            // Act
            var similarity = _service.CosineSimilarity(vec1, vec2);

            // Assert
            Assert.Equal(1.0, similarity);
        }

        [Fact]
        public void CosineSimilarity_OppositeVectors_ReturnsNegativeOne()
        {
            // Arrange
            var vec1 = new float[] { 1, 0, 0 };
            var vec2 = new float[] { -1, 0, 0 };

            // Act
            var similarity = _service.CosineSimilarity(vec1, vec2);

            // Assert
            Assert.Equal(-1.0, similarity);
        }

        [Fact]
        public void CosineSimilarity_OrthogonalVectors_ReturnsZero()
        {
            // Arrange
            var vec1 = new float[] { 1, 0, 0 };
            var vec2 = new float[] { 0, 1, 0 };

            // Act
            var similarity = _service.CosineSimilarity(vec1, vec2);

            // Assert
            Assert.Equal(0.0, similarity);
        }

        [Fact]
        public void CosineSimilarity_DifferentLengthVectors_ReturnsNegativeOne()
        {
            // Arrange
            var vec1 = new float[] { 1, 0 };
            var vec2 = new float[] { 1, 0, 0 };

            // Act
            var similarity = _service.CosineSimilarity(vec1, vec2);

            // Assert — distance returns 1.0 for mismatched lengths, so similarity = 1 - 1 = 0
            Assert.Equal(0.0, similarity);
        }

        [Fact]
        public void CosineSimilarity_ZeroVector_ReturnsNegativeOne()
        {
            // Arrange
            var vec1 = new float[] { 0, 0, 0 };
            var vec2 = new float[] { 1, 0, 0 };

            // Act
            var similarity = _service.CosineSimilarity(vec1, vec2);

            // Assert — distance returns 1.0 for zero magnitude, so similarity = 1 - 1 = 0
            Assert.Equal(0.0, similarity);
        }

        #endregion

        #region CosineDistanceTests

        [Fact]
        public void CosineDistance_IdenticalVectors_ReturnsZero()
        {
            // Arrange
            var vec1 = new float[] { 1, 0, 0 };
            var vec2 = new float[] { 1, 0, 0 };

            // Act
            var distance = _service.CosineDistance(vec1, vec2);

            // Assert
            Assert.Equal(0.0, distance);
        }

        [Fact]
        public void CosineDistance_OppositeVectors_ReturnsTwo()
        {
            // Arrange
            var vec1 = new float[] { 1, 0, 0 };
            var vec2 = new float[] { -1, 0, 0 };

            // Act
            var distance = _service.CosineDistance(vec1, vec2);

            // Assert
            Assert.Equal(2.0, distance);
        }

        [Fact]
        public void CosineDistance_OrthogonalVectors_ReturnsOne()
        {
            // Arrange
            var vec1 = new float[] { 1, 0, 0 };
            var vec2 = new float[] { 0, 1, 0 };

            // Act
            var distance = _service.CosineDistance(vec1, vec2);

            // Assert
            Assert.Equal(1.0, distance);
        }

        [Fact]
        public void CosineDistance_DifferentLengthVectors_ReturnsOne()
        {
            // Arrange
            var vec1 = new float[] { 1, 0 };
            var vec2 = new float[] { 1, 0, 0 };

            // Act
            var distance = _service.CosineDistance(vec1, vec2);

            // Assert
            Assert.Equal(1.0, distance);
        }

        [Fact]
        public void CosineDistance_ZeroVector_ReturnsOne()
        {
            // Arrange
            var vec1 = new float[] { 0, 0, 0 };
            var vec2 = new float[] { 1, 0, 0 };

            // Act
            var distance = _service.CosineDistance(vec1, vec2);

            // Assert
            Assert.Equal(1.0, distance);
        }

        #endregion

        #region MeanReciprocalRankTests

        [Fact]
        public void MeanReciprocalRank_WithNoRelevantRanks_ReturnsZero()
        {
            // Arrange
            var ranks = new List<int?>();

            // Act
            var mrr = _service.MeanReciprocalRank(ranks);

            // Assert
            Assert.Equal(0.0, mrr);
        }

        [Fact]
        public void MeanReciprocalRank_WithRelevantRanks_ReturnsCorrectMRR()
        {
            // Arrange
            var ranks = new List<int?> { 1, 2, null, 4 };
            var expectedMRR = (1.0 / 1 + 1.0 / 2 + 0 + 1.0 / 4) / 4;

            // Act
            var mrr = _service.MeanReciprocalRank(ranks);
            
            // Assert
            Assert.Equal(expectedMRR, mrr);
        }

        [Fact]
        public void MeanReciprocalRank_WithAllNullRanks_ReturnsZero()
        {
            // Arrange
            var ranks = new List<int?> { null, null, null };

            // Act
            var mrr = _service.MeanReciprocalRank(ranks);

            // Assert
            Assert.Equal(0.0, mrr);
        }

        #endregion

        #region PrecisionAtKTests

        [Fact]
        public void PrecisionAtK_WithNoRelevantItems_ReturnsZero()
        {
            // Arrange
            var retrievedIds = new List<string> { "A", "B", "C" };
            var relevantIds = new List<string>();
            int k = 3;

            // Act
            var precision = _service.PrecisionAtK(retrievedIds, relevantIds, k);

            // Assert
            Assert.Equal(0.0, precision);
        }

        [Fact]
        public void PrecisionAtK_WithRelevantItems_ReturnsCorrectPrecision()
        {
            // Arrange
            var retrievedIds = new List<string> { "A", "B", "C" };
            var relevantIds = new List<string> { "A", "D" };
            int k = 3;
            var expectedPrecision = 1.0 / 3; // 1 relevant item in top 3 results

            // Act
            var precision = _service.PrecisionAtK(retrievedIds, relevantIds, k);

            // Assert
            Assert.Equal(expectedPrecision, precision);
        }

        [Fact]
        public void PrecisionAtK_WithKGreaterThanRetrieved_ReturnsCorrectPrecision()
        {
            // Arrange
            var retrievedIds = new List<string> { "A", "B" };
            var relevantIds = new List<string> { "A", "D" };
            int k = 5;
            var expectedPrecision = 1.0 / 2; // 1 relevant item in top 2 results
            // Act
            var precision = _service.PrecisionAtK(retrievedIds, relevantIds, k);
            // Assert
            Assert.Equal(expectedPrecision, precision);
        }

        #endregion

        #region RecallAtKTests

        [Fact]
        public void RecallAtK_WithNoRelevantItems_ReturnsZero()
        {
            // Arrange
            var retrievedIds = new List<string> { "A", "B", "C" };
            var relevantIds = new List<string>();
            int k = 3;

            // Act
            var recall = _service.RecallAtK(retrievedIds, relevantIds, k);

            // Assert
            Assert.Equal(0.0, recall);
        }

        [Fact]
        public void RecallAtK_WithRelevantItems_ReturnsCorrectRecall()
        {
            // Arrange
            var retrievedIds = new List<string> { "A", "B", "C" };
            var relevantIds = new List<string> { "A", "D" };
            int k = 3;
            var expectedRecall = 1.0 / 2; // 1 relevant item retrieved out of 2 total relevant items

            // Act
            var recall = _service.RecallAtK(retrievedIds, relevantIds, k);

            // Assert
            Assert.Equal(expectedRecall, recall);
        }

        [Fact]
        public void RecallAtK_WithKGreaterThanRetrieved_ReturnsCorrectRecall()
        {
            // Arrange
            var retrievedIds = new List<string> { "A", "B" };
            var relevantIds = new List<string> { "A", "D" };
            int k = 5;
            var expectedRecall = 1.0 / 2; // 1 relevant item retrieved out of 2 total relevant items
            
            // Act
            var recall = _service.RecallAtK(retrievedIds, relevantIds, k);
            
            // Assert
            Assert.Equal(expectedRecall, recall);
        }

        [Fact]
        public void RecallAtK_WithNoRetrievedItems_ReturnsZero()
        {
            // Arrange
            var retrievedIds = new List<string>();
            var relevantIds = new List<string> { "A", "D" };
            int k = 5;
            
            // Act
            var recall = _service.RecallAtK(retrievedIds, relevantIds, k);
            
            // Assert
            Assert.Equal(0.0, recall);
        }

        #endregion

        #region NormalizedDiscountedCumulativeGainAtKTests

        [Fact]
        public void NormalizedDiscountedCumulativeGainAtK_WithNoRelevantItems_ReturnsZero()
        {
            // Arrange
            var relevanceScores = new List<double>();
            int k = 3;

            // Act
            var ndcg = _service.NormalizedDiscountedCumulativeGainAtK(relevanceScores, k);

            // Assert
            Assert.Equal(0.0, ndcg);
        }

        [Fact]
        public void NormalizedDiscountedCumulativeGainAtK_WithIdealOrdering_ReturnsOne()
        {
            // Arrange — scores already in descending order, DCG = IDCG
            var relevanceScores = new List<double> { 3, 2, 1 };
            int k = 3;

            // Act
            var ndcg = _service.NormalizedDiscountedCumulativeGainAtK(relevanceScores, k);

            // Assert
            Assert.Equal(1.0, ndcg);
        }

        [Fact]
        public void NormalizedDiscountedCumulativeGainAtK_WithNonIdealOrdering_ReturnsLessThanOne()
        {
            // Arrange — scores NOT in ideal order: [1, 3, 2] vs ideal [3, 2, 1]
            var relevanceScores = new List<double> { 1, 3, 2 };
            int k = 3;
            var dcg = 1.0 / Math.Log2(2) + 3.0 / Math.Log2(3) + 2.0 / Math.Log2(4);
            var idcg = 3.0 / Math.Log2(2) + 2.0 / Math.Log2(3) + 1.0 / Math.Log2(4);
            var expectedNDCG = dcg / idcg;

            // Act
            var ndcg = _service.NormalizedDiscountedCumulativeGainAtK(relevanceScores, k);

            // Assert
            Assert.Equal(expectedNDCG, ndcg);
            Assert.True(ndcg < 1.0);
        }

        [Fact]
        public void NormalizedDiscountedCumulativeGainAtK_WithKGreaterThanScores_ReturnsCorrectNDCG()
        {
            // Arrange — scores already in ideal order, k exceeds list length
            var relevanceScores = new List<double> { 3, 2 };
            int k = 5;

            // Act
            var ndcg = _service.NormalizedDiscountedCumulativeGainAtK(relevanceScores, k);

            // Assert
            Assert.Equal(1.0, ndcg);
        }

        [Fact]
        public void NormalizedDiscountedCumulativeGainAtK_WithAllZeroScores_ReturnsZero()
        {
            // Arrange
            var relevanceScores = new List<double> { 0, 0, 0 };
            int k = 3;

            // Act
            var ndcg = _service.NormalizedDiscountedCumulativeGainAtK(relevanceScores, k);

            // Assert
            Assert.Equal(0.0, ndcg);
        }

        #endregion

        #region CalculateQueryMetricsTests

        [Fact]
        public void CalculateQueryMetrics_WithNoResults_ReturnsZeroMetrics()
        {
            // Arrange
            var results = new List<QueryResult>();
            int topK = 3;
            var groundTruthDocumentIds = new List<Guid>();
            
            // Act
            var metrics = _service.CalculateQueryMetrics(results, topK, groundTruthDocumentIds);
            
            // Assert
            Assert.Equal(0.0, metrics.MRR);
            Assert.Equal(0.0, metrics.PrecisionAtK);
            Assert.Equal(0.0, metrics.RecallAtK);
            Assert.Equal(0.0, metrics.NDCGAtK);
        }

        [Fact]
        public void CalculateQueryMetrics_WithResults_ReturnsCorrectMetrics()
        {
            // Arrange
            var docId1 = Guid.NewGuid();
            var docId2 = Guid.NewGuid();
            var docId3 = Guid.NewGuid();
            var chunkId1 = Guid.NewGuid();
            var chunkId2 = Guid.NewGuid();
            var chunkId3 = Guid.NewGuid();

            var results = new List<QueryResult>
            {
                new QueryResult { DocumentId = docId1, DocumentChunkId = chunkId1, Rank = 1, IsRelevant = true },
                new QueryResult { DocumentId = docId2, DocumentChunkId = chunkId2, Rank = 2, IsRelevant = false },
                new QueryResult { DocumentId = docId3, DocumentChunkId = chunkId3, Rank = 3, IsRelevant = true }
            };
            int topK = 3;
            var groundTruthDocumentIds = new List<Guid> { docId1, docId3 };

            // Act
            var metrics = _service.CalculateQueryMetrics(results, topK, groundTruthDocumentIds);

            // Assert
            Assert.Equal(1.0, metrics.MRR); // First relevant result is at rank 1
            Assert.Equal(2.0 / 3.0, metrics.PrecisionAtK); // 2 relevant chunks out of 3 retrieved chunks
            Assert.Equal(1.0, metrics.RecallAtK); // All 2 ground truth documents retrieved
            Assert.True(metrics.NDCGAtK > 0.0); // NDCG should be greater than 0 for relevant items
        }

        [Fact]
        public void CalculateQueryMetrics_WithMixedRelevance_ReturnsCorrectPrecision()
        {
            // Arrange
            var docId1 = Guid.NewGuid();
            var docId2 = Guid.NewGuid();
            var docId3 = Guid.NewGuid();
            var docId4 = Guid.NewGuid();
            var chunkId1 = Guid.NewGuid();
            var chunkId2 = Guid.NewGuid();
            var chunkId3 = Guid.NewGuid();

            var results = new List<QueryResult>
            {
                new QueryResult { DocumentId = docId1, DocumentChunkId = chunkId1, Rank = 1, IsRelevant = true },
                new QueryResult { DocumentId = docId2, DocumentChunkId = chunkId2, Rank = 2, IsRelevant = true },
                new QueryResult { DocumentId = docId3, DocumentChunkId = chunkId3, Rank = 3, IsRelevant = false }
            };
            int topK = 3;
            var groundTruthDocumentIds = new List<Guid> { docId1, docId2, docId4 }; // docId4 not in results

            // Act
            var metrics = _service.CalculateQueryMetrics(results, topK, groundTruthDocumentIds);

            // Assert
            Assert.Equal(1.0, metrics.MRR); // First relevant result is at rank 1
            Assert.Equal(2.0 / 3.0, metrics.PrecisionAtK); // 2 relevant chunks out of 3 retrieved chunks
            Assert.Equal(2.0 / 3.0, metrics.RecallAtK); // 2 out of 3 ground truth documents retrieved
            Assert.True(metrics.NDCGAtK > 0.0); // NDCG should be greater than 0 for relevant items
        }
        [Fact]
        public void CalculateQueryMetrics_WithNoRelevantResults_ReturnsZeroMetrics()
        {
            // Arrange
            var docId1 = Guid.NewGuid();
            var docId2 = Guid.NewGuid();
            var docId3 = Guid.NewGuid();
            var chunkId1 = Guid.NewGuid();
            var chunkId2 = Guid.NewGuid();
            var chunkId3 = Guid.NewGuid();
            var results = new List<QueryResult>
            {
                new QueryResult { DocumentId = docId1, DocumentChunkId = chunkId1, Rank = 1, IsRelevant = false },
                new QueryResult { DocumentId = docId2, DocumentChunkId = chunkId2, Rank = 2, IsRelevant = false },
                new QueryResult { DocumentId = docId3, DocumentChunkId = chunkId3, Rank = 3, IsRelevant = false }
            };
            int topK = 3;
            var groundTruthDocumentIds = new List<Guid> { docId1, docId2, docId3 };
            // Act
            var metrics = _service.CalculateQueryMetrics(results, topK, groundTruthDocumentIds);
            // Assert
            Assert.Equal(0.0, metrics.MRR); // No relevant results
            Assert.Equal(0.0, metrics.PrecisionAtK); // No relevant chunks retrieved
            Assert.Equal(0.0, metrics.RecallAtK); // No relevant documents retrieved
            Assert.Equal(0.0, metrics.NDCGAtK); // NDCG should be 0 for no relevant items
        }

        #endregion
    }
}
