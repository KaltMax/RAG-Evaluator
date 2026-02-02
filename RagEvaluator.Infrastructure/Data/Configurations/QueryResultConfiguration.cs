using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RagEvaluator.Domain.Entities;

namespace RagEvaluator.Infrastructure.Data.Configurations
{
    /// <summary>
    /// EF Core configuration for the QueryResult entity.
    /// </summary>
    public class QueryResultConfiguration : IEntityTypeConfiguration<QueryResult>
    {
        public void Configure(EntityTypeBuilder<QueryResult> builder)
        {
            builder.ToTable("QueryResults");

            builder.HasKey(r => r.Id);

            builder.Property(r => r.QueryId)
                .IsRequired();

            // Denormalized chunk data (no FK constraints - preserved for historical accuracy)
            builder.Property(r => r.DocumentChunkId)
                .IsRequired();

            builder.Property(r => r.DocumentId)
                .IsRequired();

            builder.Property(r => r.FileName)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(r => r.ChunkText)
                .IsRequired();

            builder.Property(r => r.ChunkingStrategy)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(r => r.EmbeddingModel)
                .IsRequired()
                .HasMaxLength(100);

            // Retrieval metadata
            builder.Property(r => r.Rank)
                .IsRequired();

            builder.Property(r => r.SimilarityScore)
                .IsRequired();

            // Relevance labeling (nullable - for metrics calculation)
            builder.Property(r => r.IsRelevant);
            builder.Property(r => r.RelevanceGrade);

            // Index for efficient querying by QueryId
            builder.HasIndex(r => r.QueryId);

            // Index for analysis by DocumentId (e.g., "which queries retrieved this document?")
            builder.HasIndex(r => r.DocumentId);
        }
    }
}
