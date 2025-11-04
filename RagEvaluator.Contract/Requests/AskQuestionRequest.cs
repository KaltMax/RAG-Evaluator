using System.ComponentModel.DataAnnotations;

namespace RagEvaluator.Contract.Requests
{
    public class AskQuestionRequest
    {
        [Required]
        [MinLength(3)]
        public required string Question { get; set; }

        public int TopK { get; set; } = 3;
    }
}
