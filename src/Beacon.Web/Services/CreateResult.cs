using Beacon.Contracts.Applications;

namespace Beacon.Web.Services;

public sealed record CreateResult(ApplicationResponse? Created, IDictionary<string, string[]>? Errors);