using Beacon.Domain.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pgvector;

namespace Beacon.Infrastructure.Data.Configurations;

internal sealed class DocumentChunkConfiguration : IEntityTypeConfiguration<DocumentChunk>
{
    public const int EmbeddingDimensions = 768;
    public const string EmbeddingProperty = "Embedding";

    public void Configure(EntityTypeBuilder<DocumentChunk> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.Text).IsRequired();

        builder.HasIndex(c => new { c.DocumentId, c.Index }).IsUnique();

        builder.Property<Vector?>(EmbeddingProperty)
            .HasColumnType($"vector({EmbeddingDimensions})");

        builder.HasIndex(EmbeddingProperty)
            .HasMethod("hnsw")
            .HasOperators("vector_cosine_ops");
    }
}