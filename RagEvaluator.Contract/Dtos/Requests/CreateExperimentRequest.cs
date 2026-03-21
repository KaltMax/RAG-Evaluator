using System.ComponentModel.DataAnnotations;

namespace RagEvaluator.Contract.Dtos.Requests
{
    /// <summary>
    /// Represents a request to create a new experiment.
    /// Contains the experiment name, repeat count, and a list of queries to be processed in the experiment.
    /// </summary>
    public class CreateExperimentRequest
    {
        [Required]
        [MaxLength(200)]
        public required string Name { get; set; }

        [Required]
        [Range(1, 20)]
        public required int RepeatCount { get; set; }

        [Required]
        [MinLength(1)]
        public required List<ExperimentQueryItem> Queries { get; set; }
    }

    /// <summary>
    /// Represents a single query item in an experiment, including the question, language, and top-k retrieval parameter.
    /// </summary>
    public class ExperimentQueryItem
    {
        [Required]
        [MinLength(3)]
        public required string Question { get; set; }

        [Required]
        [RegularExpression("^(en|de)$", ErrorMessage = "Language must be 'en' or 'de'.")]
        public required string Language { get; set; }

        [Range(1, 100)]
        public int TopK { get; set; } = 3;

        public List<string> RelevantDocumentNames { get; set; } = [];
    }
}
