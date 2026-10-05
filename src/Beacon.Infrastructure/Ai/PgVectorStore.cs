using Beacon.Application.Abstractions;
using Beacon.Application.Search;
using Beacon.Domain.Documents;
using Beacon.Infrastructure.Data;
using Beacon.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace Beacon.Infrastructure.Ai;

internal sealed class PgVectorStore(BeaconDbContext db) : IVectorStore
{
    public void SetEmbeddings(IReadOnlyList<DocumentChunk> chunks, IReadOnlyList<ReadOnlyMemory<float>> embeddings)
    {
        if (chunks.Count != embeddings.Count)
            throw new ArgumentException($"Got {embeddings.Count} embeddings for {chunks.Count} chunks.");

        db.ChangeTracker.DetectChanges();

        for (var i = 0; i < chunks.Count; i++)
        {
            db.Entry(chunks[i])
                .Property(DocumentChunkConfiguration.EmbeddingProperty)
                .CurrentValue = new Vector(embeddings[i]);
        }
    }

    public async Task<IReadOnlyList<ChunkMatch>> SearchAsync(
        ReadOnlyMemory<float> query, int top, Guid? applicationId, DocumentKind? kind, CancellationToken ct)
    {
        var queryVector = new Vector(query);
        const string embedding = DocumentChunkConfiguration.EmbeddingProperty;

        var rows =
            from c in db.DocumentChunks
            join d in db.Documents on c.DocumentId equals d.Id
            where d.Status == DocumentStatus.Ready
                && EF.Property<Vector?>(c, embedding) != null
            select new { Chunk = c, Document = d };

        if (applicationId is not null) rows = rows.Where(x => x.Document.JobApplicationId == applicationId);
        if (kind is not null) rows = rows.Where(x => x.Document.Kind == kind); 

        return await rows
            .OrderBy(x => EF.Property<Vector>(x.Chunk, embedding).CosineDistance(queryVector))
            .Take(top)
            .Select(x => new ChunkMatch(
                x.Document.Id,
                x.Document.FileName,
                x.Document.Kind,
                x.Chunk.Index,
                x.Chunk.Text,
                1 - EF.Property<Vector>(x.Chunk, embedding).CosineDistance(queryVector)))
            .ToListAsync(ct);
    }
}