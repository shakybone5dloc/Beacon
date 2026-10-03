using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Validation;

namespace Beacon.Contracts.Applications;

[ValidatableType]
public sealed record ChangeStatuRequest
{
    [Required]
    public string Status { get; init; } = "";
    public uint Version { get; init; }
}