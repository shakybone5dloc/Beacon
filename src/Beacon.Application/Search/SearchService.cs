using Beacon.Application.Abstractions;
using Beacon.Contracts.Search;
using Beacon.Domain.Documents;
using Microsoft.Extensions.AI;

namespace Beacon.Application.Search;

public sealed class SearchService(IEmbeddingGenerator<string, Embedding<float>> embedder, IVectorStore vectors)
{
    public const int DefaultTop = 5;
    public const int MaxTop = 20;

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        string query, int? top, Guid? applicationId, DocumentKind? kind, CancellationToken ct)
    {
        var take = Math.Clamp(top ?? DefaultTop, 1, MaxTop);

        var queryVector = await embedder.GenerateVectorAsync(query, cancellationToken: ct);
        var matches = await vectors.SearchAsync(queryVector, take, applicationId, kind, ct);
        return matches
            .Select(m => new SearchResult(
                m.DocumentId,
                m.FileName,
                m.Kind.ToString(),
                m.ChunkIndex,
                m.Text,
                Math.Round(m.Score, 4)))
            .ToList();
    }
}