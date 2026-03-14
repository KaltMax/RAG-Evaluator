using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pgvector;
using RagEvaluator.Domain.Entities;

namespace RagEvaluator.Infrastructure.Data.Configurations
{
    /// <summary>
    /// EF Core configuration for the DocumentChunk entity with pgvector conversion.
    /// </summary>
    public class DocumentChunkConfiguration : IEntityTypeConfiguration<DocumentChunk>
    {
        public void Configure(EntityTypeBuilder<DocumentChunk> builder)
        {
            builder.ToTable("DocumentChunks");

            builder.HasKey(c => c.Id);

            builder.Property(c => c.Text)
                .IsRequired();

            var floatArrayComparer = new ValueComparer<float[]>(
                (a, b) => a != null && b != null && a.SequenceEqual(b),
                v => v.Aggregate(0, (hash, el) => HashCode.Combine(hash, el.GetHashCode())),
                v => v.ToArray());

            builder.Property(c => c.Embedding)
                .HasConversion(
                    v => new Vector(v),
                    v => v.ToArray(),
                    floatArrayComparer)
                .IsRequired();

            builder.Property(c => c.ChunkingStrategy)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(c => c.EmbeddingModel)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(c => c.DocumentId)
                .IsRequired();

            builder.HasOne<Document>()
                .WithMany()
                .HasForeignKey(c => c.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(c => c.DocumentId);
        }
    }
}
