using Beacon.Application.Abstractions;
using Beacon.Contracts.Documents;
using Beacon.Domain.Documents;
using Microsoft.EntityFrameworkCore;

namespace Beacon.Application.Documents;

public sealed class DocumentService(IBeaconDbContext db, IDocumentQueue queue, TimeProvider time)
{
    public async Task<UploadResult> UploadAsync(
        string fileName, DocumentKind kind, string content, Guid? applicationId, CancellationToken ct)
    {
        if (applicationId is { } appId && !await db.JobApplications.AnyAsync(a => a.Id == appId, ct))
            return UploadResult.Fail($"Application '{appId}' does not exist.");

        var document = Document.Create(fileName, kind, content, applicationId, time.GetUtcNow());
        db.Documents.Add(document);
        await db.SaveChangesAsync(ct);

        await queue.EnqueueAsync(document.Id, ct);

        return UploadResult.Ok(document.ToResponse(chunkCount: 0));
    }

    public async Task<DocumentResponse?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var document = await db.Documents
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id, ct);

        if (document is null) return null;

        var count = await db.DocumentChunks.CountAsync(c => c.DocumentId == id, ct);

        return document.ToResponse(count);
    }

    public async Task<IReadOnlyList<DocumentResponse>?> ListForApplicationAsync(Guid applicationId, CancellationToken ct)
    {
        if (!await db.JobApplications.AnyAsync(a => a.Id == applicationId, ct))
            return null;

        var documents = await db.Documents
            .AsNoTracking()
            .Where(d => d.JobApplicationId == applicationId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(ct);

        var ids = documents.Select(d => d.Id).ToList();
        var counts = await db.DocumentChunks
            .Where(c => ids.Contains(c.DocumentId))
            .GroupBy(c => c.DocumentId)
            .Select(g => new { DocumentId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.DocumentId, x => x.Count, ct);

        return documents
            .Select(d => d.ToResponse(counts.GetValueOrDefault(d.Id)))
            .ToList();
    }
}