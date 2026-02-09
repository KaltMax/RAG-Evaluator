using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RagEvaluator.Domain.Entities;
using RagEvaluator.Domain.Enums;

namespace RagEvaluator.Infrastructure.Data.Configurations
{
    /// <summary>
    /// EF Core configuration for the Experiment entity,
    /// </summary>
    public class ExperimentConfiguration : IEntityTypeConfiguration<Experiment>
    {
        public void Configure(EntityTypeBuilder<Experiment> builder)
        {
            builder.ToTable("Experiments");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(e => e.RepeatCount)
                .IsRequired();

            builder.Property(e => e.Status)
                .IsRequired()
                .HasConversion(
                    v => v.ToString(),
                    v => Enum.Parse<ExperimentStatus>(v));

            builder.Property(e => e.CreatedAt)
                .IsRequired();

            builder.Property(e => e.CompletedAt);

            // Config snapshot
            builder.Property(e => e.EmbeddingModel)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(e => e.ChunkingStrategy)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(e => e.ChatModel)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(e => e.ChunkSize)
                .IsRequired();

            builder.Property(e => e.ChunkOverlap)
                .IsRequired();

            builder.Property(e => e.SimilarityThreshold)
                .IsRequired();

            builder.Property(e => e.PromptTemplate)
                .IsRequired()
                .HasMaxLength(100);

            // Progress
            builder.Property(e => e.TotalQueryCount)
                .IsRequired();

            builder.Property(e => e.CompletedQueryCount)
                .IsRequired();

            builder.HasMany(e => e.Queries)
                .WithOne(q => q.Experiment)
                .HasForeignKey(q => q.ExperimentId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
