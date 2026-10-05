namespace Beacon.Contracts.Search;

public sealed record SearchResult(
    Guid DocumentId,
    string FileName,
    string Kind,
    int ChunkIndex,
    string Text,
    double Score);