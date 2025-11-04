namespace RagEvaluator.Contract.Responses
{
    public class SearchResultDto
    {
        public int Id { get; set; }
        public required string Text { get; set; }
        public float Similarity { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
    }
}
