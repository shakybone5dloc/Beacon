using Beacon.Domain.Documents;
using Beacon.Application.Search;

namespace Beacon.Application.Abstractions;

public interface IVectorStore
{
    void SetEmbeddings(IReadOnlyList<DocumentChunk> chunks, IReadOnlyList<ReadOnlyMemory<float>> embeddings);

    Task<IReadOnlyList<ChunkMatch>> SearchAsync(
        ReadOnlyMemory<float> query, int top, Guid? applicationId, DocumentKind? kind, CancellationToken ct);
}