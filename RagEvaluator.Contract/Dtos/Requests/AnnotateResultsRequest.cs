using System.ComponentModel.DataAnnotations;
using RagEvaluator.Domain.Enums;

namespace RagEvaluator.Contract.Dtos.Requests
{
    /// <summary>
    /// Represents a request to annotate query results with relevance labels and response quality evaluation.
    /// </summary>
    public class AnnotateResultsRequest
    {
        [Required]
        [MinLength(1, ErrorMessage = "At least one annotation is required.")]
        public required List<QueryResultAnnotation> Annotations { get; set; }

        [Required]
        [EnumDataType(typeof(ResponseQuality))]
        public required ResponseQuality ResponseQuality { get; set; }
        
        [Required]
        public required bool HasLanguageSwitching { get; set; }

        public List<Guid> RelevantDocumentIds { get; set; } = [];
    }

    /// <summary>
    /// Represents a relevance annotation for a single query result.
    /// </summary>
    public class QueryResultAnnotation
    {
        [Required]
        public required Guid ResultId { get; set; }

        [Required]
        [EnumDataType(typeof(RelevanceGrade))]
        public RelevanceGrade RelevanceGrade { get; set; }
    }
}
