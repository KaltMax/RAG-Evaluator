namespace RagEvaluator.Contract.Dtos.Responses
{
    /// <summary>
    /// Response returned after queuing documents for reprocessing. Processing happens asynchronously;
    /// per-document progress is delivered via job notifications.
    /// </summary>
    public class ReprocessResponse
    {
        public int DocumentsQueued { get; set; }
    }
}
