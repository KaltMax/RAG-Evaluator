namespace RagEvaluator.Domain.Enums
{
    /// <summary>
    /// Defines the possible statuses of an experiment, indicating whether it is currently running,
    /// has completed, or failed during background processing.
    /// </summary>
    public enum ExperimentStatus
    {
        Running,
        Completed,
        Failed
    }
}
