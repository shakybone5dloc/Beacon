using Beacon.Application.Abstractions;
using Beacon.Domain.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Beacon.Application.Documents;

public sealed class DocumentProcessingService(
    IBeaconDbContext db, TimeProvider time, ILogger<DocumentProcessingService> logger)
{
    public async Task ProcessAsync(Guid documentId, CancellationToken ct)
    {
        var document = await db.Documents
            .Include(d => d.Chunks)
            .FirstOrDefaultAsync(d => d.Id == documentId, ct);

        if (document is null)
        {
            logger.LogWarning("Document {DocumentId} not found; skipping", documentId);
            return;
        }

        if (document.Status is not (DocumentStatus.Pending or DocumentStatus.Processing))
            return;

        document.StartProcessing(time.GetUtcNow());
        await db.SaveChangesAsync(ct);

        var chunks = TextChunker.Chunk(document.Content);

        if (chunks.Count == 0)
            document.MarkFailed("Document contains no text", time.GetUtcNow());
        else
            document.MarkReady(chunks, time.GetUtcNow());

        await db.SaveChangesAsync(ct);

        logger.LogInformation("Document {DocumentId} finished as {Status} with {ChunkCount} chunks",
            document.Id, document.Status, document.Chunks.Count);
    }
}