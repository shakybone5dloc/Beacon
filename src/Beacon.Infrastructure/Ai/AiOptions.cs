using System.ComponentModel.DataAnnotations;

namespace Beacon.Infrastructure.Ai;

public sealed class AiOptions
{
    public const string SectionName = "Ai";

    [Required, Url] public string Endpoint { get; init; } = "";
    [Required] public string EmbeddingModel { get; init; } = "";
    [Range(1, 4096)] public int EmbeddingDimensions { get; init; }
}