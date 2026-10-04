namespace Beacon.Contracts.Documents;

public sealed record DocumentResponse(
    Guid Id,
    string FileName,
    string Kind,
    string Status,
    Guid? JobApplicationId,
    int ChunkCount,
    string? Error,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);