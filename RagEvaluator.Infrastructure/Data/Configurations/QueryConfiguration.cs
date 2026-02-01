using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
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

            builder.Property(q => q.EmbeddingModel)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(q => q.ChatModel)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(q => q.CreatedAt)
                .IsRequired();
        }
    }
}
