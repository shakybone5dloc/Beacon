using Beacon.Application.Documents;
using Beacon.Contracts.Documents;
using Beacon.Domain.Documents;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Beacon.Api.Endpoints;

public static class DocumentEndpoints
{
    private static readonly string[] AllowedExtensions = [".txt", ".md"];
    private const long MaxBytes = 2 * 1024 * 1024;

    public static IEndpointRouteBuilder MapDocumentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/documents").WithTags("Documents");

        group.MapPost("/", UploadAsync)
            .DisableAntiforgery();
        group.MapGet("/{id:guid}", GetByIdAsync);

        app.MapGet("/api/applications/{id:guid}/documents", ListForApplicationAsync)
            .WithTags("Documents");

        return app;
    }

    private static async Task<Results<Accepted<DocumentResponse>, ValidationProblem>> UploadAsync(
        IFormFile file,
        [FromForm] string kind,
        [FromForm] Guid? applicationId,
        DocumentService service,
        CancellationToken ct)
    {
        var errors = new Dictionary<string, string[]>();
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

        var fileErrors = new List<string>();

        if (!AllowedExtensions.Contains(fileExtension))
            fileErrors.Add($"'{fileExtension}' is not an allowed file type. Use .txt or .md.");

        if (file.Length == 0 || file.Length > MaxBytes)
            fileErrors.Add($"File must be between 1 byte and 2 MB (was {file.Length} bytes).");

        if (fileErrors.Count > 0)
            errors["file"] = fileErrors.ToArray();

        if (!Enum.TryParse<DocumentKind>(kind, ignoreCase: true, out var parsedKind) || !Enum.IsDefined(parsedKind))
            errors["kind"] = [$"'{kind}' is not a valid document kind."];

        if (errors.Count > 0)
            return TypedResults.ValidationProblem(errors);

        string content;
        using (var reader = new StreamReader(file.OpenReadStream()))
            content = await reader.ReadToEndAsync(ct);

        var result = await service.UploadAsync(file.FileName, parsedKind, content, applicationId, ct);

        if (result.Error is not null)
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["applicationId"] = [result.Error]
            });

        return TypedResults.Accepted($"/api/documents/{result.Document!.Id}", result.Document);
    }

    private static async Task<Results<Ok<DocumentResponse>, NotFound>> GetByIdAsync(
        Guid id, DocumentService service, CancellationToken ct) =>
        await service.GetByIdAsync(id, ct) is { } document
        ? TypedResults.Ok(document) : TypedResults.NotFound();


    private static async Task<Results<Ok<IReadOnlyList<DocumentResponse>>, NotFound>> ListForApplicationAsync(
        Guid id, DocumentService service, CancellationToken ct) =>
            await service.ListForApplicationAsync(id, ct) is { } results
            ? TypedResults.Ok(results) : TypedResults.NotFound();

}