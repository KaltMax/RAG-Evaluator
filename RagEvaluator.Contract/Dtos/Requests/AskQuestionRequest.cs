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

        [Required]
        public required string Language { get; set; }

        [Required]
        [Range(1, 100)]
        public required int TopK { get; set; } = 3;
    }
}
