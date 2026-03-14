using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pgvector;
using RagEvaluator.Domain.Entities;

namespace RagEvaluator.Infrastructure.Data.Configurations
{
    /// <summary>
    /// EF Core configuration for the Query entity.
    /// </summary>
    public class QueryConfiguration : IEntityTypeConfiguration<Query>
    {
        public void Configure(EntityTypeBuilder<Query> builder)
        {
            builder.ToTable("Queries");

            builder.HasKey(q => q.Id);

            // Query parameters
            builder.Property(q => q.Question)
                .IsRequired()
                .HasMaxLength(2000);

            builder.Property(q => q.Language)
                .IsRequired()
                .HasMaxLength(10);

            builder.Property(q => q.TopK)
                .IsRequired();

            builder.Property(q => q.SystemPrompt)
                .IsRequired();

            builder.Property(q => q.ChunkingStrategy)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(q => q.EmbeddingModel)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(q => q.ChatModel)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(q => q.CreatedAt)
                .IsRequired();

            // Response data
            builder.Property(q => q.Answer)
                .IsRequired();

            var floatArrayComparer = new ValueComparer<float[]>(
                (a, b) => a != null && b != null && a.SequenceEqual(b),
                v => v.Aggregate(0, (hash, el) => HashCode.Combine(hash, el.GetHashCode())),
                v => v.ToArray());

            builder.Property(q => q.QueryEmbedding)
                .HasConversion(
                    v => new Vector(v),
                    v => v.ToArray(),
                    floatArrayComparer)
                .IsRequired();

            builder.Property(q => q.ResponseTimeMs)
                .IsRequired();

            // Response Quality Evaluation (nullable)
            builder.Property(q => q.ResponseQuality);
            builder.Property(q => q.HasLanguageSwitching);

            // Retrieval Metrics (nullable)
            builder.Property(q => q.MRR);
            builder.Property(q => q.PrecisionAtK);
            builder.Property(q => q.RecallAtK);
            builder.Property(q => q.NDCGAtK);

            // Experiment association
            builder.HasIndex(q => q.ExperimentId);

            // Navigation to QueryResults
            builder.HasMany(q => q.Results)
                .WithOne(r => r.Query)
                .HasForeignKey(r => r.QueryId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
