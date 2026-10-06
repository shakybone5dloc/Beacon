using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Validation;

namespace Beacon.Contracts.Ask;

[ValidatableType]
public sealed record AskRequest
{
    [Required, StringLength(1000)]
    public string Question { get; init; } = "";

    public Guid? ApplicationId { get; init; }
}