using RagEvaluator.Application.Mappers;
using RagEvaluator.Domain.Entities;
using RagEvaluator.Domain.Enums;

namespace RagEvaluator.Test.ApplicationTest
{
    public class ExperimentMetricsAggregatorTests
    {
        #region BuildQueryGroup Tests

        [Fact]
        public void BuildQueryGroup_ShouldMapQuestionLanguageAndTopK()
        {
            // Arrange
            var queries = new List<Query> { CreateSampleQuery() };

            // Act
            var result = ExperimentMetricsAggregator.BuildQueryGroup("What is cloud?", "en", 5, queries);

            // Assert
            Assert.Equal("What is cloud?", result.Question);
            Assert.Equal("en", result.Language);
            Assert.Equal(5, result.TopK);
            Assert.Single(result.QueryIds);
        }

        [Fact]
        public void BuildQueryGroup_WithAnnotatedQueries_ShouldIncludeMetrics()
        {
            // Arrange
            var queries = new List<Query>
            {
                CreateSampleQuery(responseQuality: ResponseQuality.CorrectAndComplete, mrr: 1.0),
                CreateSampleQuery(responseQuality: ResponseQuality.VagueOrIncomplete, mrr: 0.5)
            };

            // Act
            var result = ExperimentMetricsAggregator.BuildQueryGroup("Q", "en", 3, queries);

            // Assert
            Assert.Equal(2, result.AnnotatedCount);
            Assert.NotNull(result.Metrics);
        }

        [Fact]
        public void BuildQueryGroup_WithNoAnnotatedQueries_ShouldHaveNullMetrics()
        {
            // Arrange
            var queries = new List<Query> { CreateSampleQuery() };

            // Act
            var result = ExperimentMetricsAggregator.BuildQueryGroup("Q", "en", 3, queries);

            // Assert
            Assert.Equal(0, result.AnnotatedCount);
            Assert.Null(result.Metrics);
        }

        #endregion

        #region ComputeAggregatedMetrics Tests

        [Fact]
        public void ComputeAggregatedMetrics_ShouldCalculateMeanResponseTime()
        {
            // Arrange
            var queries = new List<Query>
            {
                CreateSampleQuery(responseQuality: ResponseQuality.CorrectAndComplete, responseTimeMs: 100),
                CreateSampleQuery(responseQuality: ResponseQuality.CorrectAndComplete, responseTimeMs: 200),
                CreateSampleQuery(responseQuality: ResponseQuality.CorrectAndComplete, responseTimeMs: 300)
            };

            // Act
            var result = ExperimentMetricsAggregator.ComputeAggregatedMetrics(queries);

            // Assert
            Assert.Equal(200.0, result.ResponseTimeMs!.Mean);
        }

        [Fact]
        public void ComputeAggregatedMetrics_ShouldCalculateStdDev()
        {
            // Arrange — values [100, 200, 300], mean=200
            var queries = new List<Query>
            {
                CreateSampleQuery(responseQuality: ResponseQuality.CorrectAndComplete, responseTimeMs: 100),
                CreateSampleQuery(responseQuality: ResponseQuality.CorrectAndComplete, responseTimeMs: 200),
                CreateSampleQuery(responseQuality: ResponseQuality.CorrectAndComplete, responseTimeMs: 300)
            };

            // Act
            var result = ExperimentMetricsAggregator.ComputeAggregatedMetrics(queries);

            // Assert — sample stddev: sqrt(((100-200)^2 + (0)^2 + (100)^2) / 2) = sqrt(10000) = 100
            Assert.Equal(100.0, result.ResponseTimeMs!.StdDev);
        }

        [Fact]
        public void ComputeAggregatedMetrics_WithSingleQuery_ShouldHaveZeroStdDev()
        {
            // Arrange
            var queries = new List<Query>
            {
                CreateSampleQuery(responseQuality: ResponseQuality.CorrectAndComplete, responseTimeMs: 150)
            };

            // Act
            var result = ExperimentMetricsAggregator.ComputeAggregatedMetrics(queries);

            // Assert
            Assert.Equal(150.0, result.ResponseTimeMs!.Mean);
            Assert.Equal(0.0, result.ResponseTimeMs.StdDev);
        }

        [Fact]
        public void ComputeAggregatedMetrics_ShouldCalculateMeanMetrics()
        {
            // Arrange
            var queries = new List<Query>
            {
                CreateSampleQuery(responseQuality: ResponseQuality.CorrectAndComplete, mrr: 1.0, precision: 0.8, recall: 1.0, ndcg: 0.9),
                CreateSampleQuery(responseQuality: ResponseQuality.CorrectAndComplete, mrr: 0.5, precision: 0.6, recall: 0.5, ndcg: 0.7)
            };

            // Act
            var result = ExperimentMetricsAggregator.ComputeAggregatedMetrics(queries);

            // Assert
            Assert.Equal(0.75, result.MRR!.Mean);
            Assert.Equal(0.7, result.PrecisionAtK!.Mean);
            Assert.Equal(0.75, result.RecallAtK!.Mean);
            Assert.Equal(0.8, result.NDCGAtK!.Mean);
        }

        [Fact]
        public void ComputeAggregatedMetrics_WithNoMetricValues_ShouldReturnNullMeans()
        {
            // Arrange — queries with ResponseQuality but no IR metrics
            var queries = new List<Query>
            {
                CreateSampleQuery(responseQuality: ResponseQuality.CorrectAndComplete)
            };

            // Act
            var result = ExperimentMetricsAggregator.ComputeAggregatedMetrics(queries);

            // Assert
            Assert.Null(result.MRR);
            Assert.Null(result.PrecisionAtK);
            Assert.Null(result.RecallAtK);
            Assert.Null(result.NDCGAtK);
        }

        [Fact]
        public void ComputeAggregatedMetrics_ShouldCalculateResponseQualityDistribution()
        {
            // Arrange
            var queries = new List<Query>
            {
                CreateSampleQuery(responseQuality: ResponseQuality.CorrectAndComplete),
                CreateSampleQuery(responseQuality: ResponseQuality.CorrectAndComplete),
                CreateSampleQuery(responseQuality: ResponseQuality.VagueOrIncomplete),
                CreateSampleQuery(responseQuality: ResponseQuality.Hallucinated)
            };

            // Act
            var result = ExperimentMetricsAggregator.ComputeAggregatedMetrics(queries);

            // Assert
            Assert.NotNull(result.ResponseQualityDistribution);
            Assert.Equal(2, result.ResponseQualityDistribution["CorrectAndComplete"]);
            Assert.Equal(1, result.ResponseQualityDistribution["VagueOrIncomplete"]);
            Assert.Equal(1, result.ResponseQualityDistribution["Hallucinated"]);
        }

        [Fact]
        public void ComputeAggregatedMetrics_ShouldCalculateLanguageSwitchingRate()
        {
            // Arrange — 1 out of 4 has language switching
            var queries = new List<Query>
            {
                CreateSampleQuery(responseQuality: ResponseQuality.CorrectAndComplete, hasLanguageSwitching: false),
                CreateSampleQuery(responseQuality: ResponseQuality.CorrectAndComplete, hasLanguageSwitching: false),
                CreateSampleQuery(responseQuality: ResponseQuality.CorrectAndComplete, hasLanguageSwitching: true),
                CreateSampleQuery(responseQuality: ResponseQuality.CorrectAndComplete, hasLanguageSwitching: false)
            };

            // Act
            var result = ExperimentMetricsAggregator.ComputeAggregatedMetrics(queries);

            // Assert
            Assert.Equal(0.25, result.LanguageSwitchingRate);
        }

        [Fact]
        public void ComputeAggregatedMetrics_WithNoLanguageSwitchingData_ShouldReturnNull()
        {
            // Arrange
            var queries = new List<Query>
            {
                CreateSampleQuery(responseQuality: ResponseQuality.CorrectAndComplete)
            };

            // Act
            var result = ExperimentMetricsAggregator.ComputeAggregatedMetrics(queries);

            // Assert
            Assert.Null(result.LanguageSwitchingRate);
        }

        #endregion

        #region Helper Methods

        private Query CreateSampleQuery(
            ResponseQuality? responseQuality = null,
            int responseTimeMs = 100,
            double? mrr = null,
            double? precision = null,
            double? recall = null,
            double? ndcg = null,
            bool? hasLanguageSwitching = null)
        {
            return new Query
            {
                Id = Guid.NewGuid(),
                Question = "What is cloud?",
                Language = "en",
                TopK = 3,
                ResponseTimeMs = responseTimeMs,
                ResponseQuality = responseQuality,
                MRR = mrr,
                PrecisionAtK = precision,
                RecallAtK = recall,
                NDCGAtK = ndcg,
                HasLanguageSwitching = hasLanguageSwitching
            };
        }

        #endregion
    }
}
