using System.ComponentModel.DataAnnotations;

namespace Beacon.Api.RateLimiting;

public sealed class AskRateLimitOptions
{
    public const string SectionName = "RateLimiting:Ask";
    public const string PolicyName = "ask";

    [Range(1, 10_000)] public int PermitLimit { get; init; }
    [Range(1, 3600)] public int WindowSeconds { get; init; }
}