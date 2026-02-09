namespace RagEvaluator.Contract.Dtos.Responses
{
    public class ExperimentResponse
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public required string Status { get; set; }
        public int RepeatCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        // Configuration snapshot
        public required string EmbeddingModel { get; set; }
        public required string ChunkingStrategy { get; set; }
        public required string ChatModel { get; set; }
        public int ChunkSize { get; set; }
        public int ChunkOverlap { get; set; }
        public double SimilarityThreshold { get; set; }
        public required string PromptTemplate { get; set; }

        // Progress
        public required ExperimentProgress Progress { get; set; }

        // Query groups (one per unique question)
        public List<ExperimentQueryGroupResponse> QueryGroups { get; set; } = [];

        // Overall aggregated metrics (only when fully annotated)
        public ExperimentAggregatedMetrics? OverallMetrics { get; set; }
    }

    public class ExperimentProgress
    {
        public int Total { get; set; }
        public int Completed { get; set; }
        public int Annotated { get; set; }
    }

    public class ExperimentQueryGroupResponse
    {
        public required string Question { get; set; }
        public required string Language { get; set; }
        public int TopK { get; set; }
        public List<Guid> QueryIds { get; set; } = [];
        public int AnnotatedCount { get; set; }
        public ExperimentAggregatedMetrics? Metrics { get; set; }
    }

    public class ExperimentAggregatedMetrics
    {
        public MetricAggregate? ResponseTimeMs { get; set; }
        public double? MeanMRR { get; set; }
        public double? MeanPrecisionAtK { get; set; }
        public double? MeanRecallAtK { get; set; }
        public double? MeanNDCGAtK { get; set; }
        public Dictionary<string, int>? ResponseQualityDistribution { get; set; }
        public double? LanguageSwitchingRate { get; set; }
    }

    public class MetricAggregate
    {
        public double Mean { get; set; }
        public double StdDev { get; set; }
    }
}
