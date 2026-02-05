using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RagEvaluator.Domain.Entities;

namespace RagEvaluator.Infrastructure.Data.Configurations
{
    /// <summary>
    /// EF Core configuration for the QueryRelevantDocument entity.
    /// </summary>
    public class QueryRelevantDocumentConfiguration : IEntityTypeConfiguration<QueryRelevantDocument>
    {
        public void Configure(EntityTypeBuilder<QueryRelevantDocument> builder)
        {
            builder.ToTable("QueryRelevantDocuments");

            builder.HasKey(qrd => new { qrd.QueryId, qrd.DocumentId });

            builder.HasOne(qrd => qrd.Query)
                .WithMany(q => q.RelevantDocuments)
                .HasForeignKey(qrd => qrd.QueryId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(qrd => qrd.DocumentId);
        }
    }
}
