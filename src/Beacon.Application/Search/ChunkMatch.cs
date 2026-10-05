using Beacon.Domain.Documents;

namespace Beacon.Application.Search;

public sealed record ChunkMatch(
    Guid DocumentId, string FileName, DocumentKind Kind, int ChunkIndex, string Text, double Score);