namespace RagEvaluator.Contract.Dtos.Notifications
{
    /// <summary>
    /// A real-time update about a background job, pushed to connected clients. Generic across job types.
    /// </summary>
    public sealed record JobNotification(
        string JobType,
        Guid EntityId,
        string Status,
        string? Name = null,
        int? Completed = null,
        int? Total = null,
        string? Message = null);
}
