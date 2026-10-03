namespace Beacon.Contracts.Applications;

public sealed record ApplicationResponse(
    Guid Id,
    string Company,
    string Role,
    string? JobUrl,
    string? Notes,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);