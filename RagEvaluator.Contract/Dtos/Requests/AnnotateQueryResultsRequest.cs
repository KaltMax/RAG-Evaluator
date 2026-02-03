using System.ComponentModel.DataAnnotations;
using RagEvaluator.Domain.Enums;

namespace RagEvaluator.Contract.Dtos.Requests
{
    /// <summary>
    /// Represents a request to annotate query results with relevance labels.
    /// </summary>
    public class AnnotateQueryResultsRequest
    {
        [Required]
        [MinLength(1, ErrorMessage = "At least one annotation is required.")]
        public required List<ResultAnnotation> Annotations { get; set; }
    }

    /// <summary>
    /// Represents a relevance annotation for a single query result.
    /// </summary>
    public class ResultAnnotation
    {
        [Required]
        public required Guid ResultId { get; set; }

        [Required]
        [EnumDataType(typeof(RelevanceGrade))]
        public RelevanceGrade RelevanceGrade { get; set; }
    }
}
