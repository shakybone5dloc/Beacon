using Beacon.Application.Search;
using Beacon.Contracts.Search;
using Beacon.Domain.Documents;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Beacon.Api.Endpoints;

public static class SearchEndpoints
{
    public static IEndpointRouteBuilder MapSearchEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/search", SearchAsync).WithTags("Search");
        return app;
    }

    private static async Task<Results<Ok<IReadOnlyList<SearchResult>>, ValidationProblem>> SearchAsync(
        string? q, Guid? applicationId, string? kind, int? top, SearchService service, CancellationToken ct)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(q))
            errors["q"] = ["A search query is required."];

        DocumentKind? parsedKind = null;
        if (kind is not null)
        {
            if (Enum.TryParse<DocumentKind>(kind, ignoreCase: true, out var k) && Enum.IsDefined(k))
                parsedKind = k;
            else
                errors["kind"] = [$"'{kind}' is not a valid document kind."];
        }

        if (errors.Count > 0)
            return TypedResults.ValidationProblem(errors);

        var results = await service.SearchAsync(q!, top, applicationId, parsedKind, ct);
        return TypedResults.Ok(results);
    }
}