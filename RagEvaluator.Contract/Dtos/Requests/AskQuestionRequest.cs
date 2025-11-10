using System.ComponentModel.DataAnnotations;

namespace RagEvaluator.Contract.Dtos.Requests
{
    /// <summary>
    /// Represents a request to ask a question, specifying the query and the number of top results to retrieve.
    /// </summary>
    public class AskQuestionRequest
    {
        [Required]
        [MinLength(3)]
        public required string Question { get; set; }

        public int TopK { get; set; } = 3;
    }
}
