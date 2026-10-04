namespace Beacon.Application.Abstractions;

public interface IDocumentQueue
{
    ValueTask EnqueueAsync(Guid documentId, CancellationToken ct = default);
}