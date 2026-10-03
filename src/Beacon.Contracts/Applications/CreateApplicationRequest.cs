using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Validation;

namespace Beacon.Contracts.Applications;

[ValidatableType]
public sealed record CreateApplicationRequest
{

    [Required, StringLength(200)]
    public string Company { get; init; } = "";

    [Required, StringLength(200)]
    public string Role { get; init; } = "";

    [Url, StringLength(2048)]
    public string? JobUrl { get; init; }

    [StringLength(4000)]
    public string? Notes { get; init; }
}