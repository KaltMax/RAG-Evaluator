using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RagEvaluator.Domain.Entities;

namespace RagEvaluator.Infrastructure.Data.Configurations
{
    public class DocumentConfiguration : IEntityTypeConfiguration<Document>
    {
        public void Configure(EntityTypeBuilder<Document> builder)
        {
            builder.ToTable("Documents");

            builder.HasKey(d => d.Id);

            builder.Property(d => d.FileName)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(d => d.FilePath)
                .HasMaxLength(500);

            builder.Property(d => d.MimeType)
                .HasMaxLength(100);

            builder.Property(d => d.FileSize);

            builder.Property(d => d.PageCount);

            builder.Property(d => d.ChunkCount);

            builder.Property(d => d.UploadedAt)
                .IsRequired();

            builder.Property(d => d.ProcessedAt);

            builder.Property(d => d.Status)
                .HasConversion<string>()
                .HasMaxLength(50);
        }
    }
}
