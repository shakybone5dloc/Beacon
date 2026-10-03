using Beacon.Application.Applications;
using Beacon.Contracts.Applications;
using Beacon.Domain.Applications;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Beacon.Api.Endpoints;

public static class ApplicationEndpoints
{
    public static IEndpointRouteBuilder MapApplicationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/applications").WithTags("Applications");

        group.MapPost("/", CreateAsync);
        group.MapGet("/{id:guid}", GetByIdAsync);
        group.MapGet("/", ListAsync);
        group.MapPatch("/{id:guid}/status", ChangeStatusAsync);

        return app;
    }

    private static async Task<Created<ApplicationResponse>> CreateAsync(
        CreateApplicationRequest request, ApplicationService service, CancellationToken ct)
    {
        var response = await service.CreateAsync(request, ct);
        return TypedResults.Created($"/api/applications/{response.Id}", response);
    }

    private static async Task<Results<Ok<ApplicationResponse>, NotFound>> GetByIdAsync(
        Guid id, ApplicationService service, CancellationToken ct) =>
        await service.GetByIdAsync(id, ct) is { } application
            ? TypedResults.Ok(application) : TypedResults.NotFound();

    private static async Task<Ok<IReadOnlyList<ApplicationResponse>>> ListAsync(
        ApplicationService service, CancellationToken ct) =>
        TypedResults.Ok(await service.ListAsync(ct));  
    
    private static async Task<Results<Ok<ApplicationResponse>, NotFound, ValidationProblem>> ChangeStatusAsync(
        Guid id, ChangeStatusRequest request, ApplicationService service, CancellationToken ct)
    {
        if (!Enum.TryParse<ApplicationStatus>(request.Status, ignoreCase: true, out var status)
            || !Enum.IsDefined(status))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["status"] = [$"'{request.Status}' is not a valid status."]
            });
        }

        return await service.ChangeStatusAsync(id, status, request.Version, ct) is { } result
            ? TypedResults.Ok(result) : TypedResults.NotFound();

    }
}