using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Beacon.Infrastructure;
using Beacon.Api.Endpoints;
using Beacon.Application;
using Beacon.Contracts;
using Beacon.Api.Errors;
using Microsoft.Extensions.AI;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ConflictExceptionHandler>();
builder.Services.AddContracts();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapGet("/dev/embed", async (
        string text,
        IEmbeddingGenerator<string, Embedding<float>> generator,
        CancellationToken ct) =>
    {
        ReadOnlyMemory<float> vector = await generator.GenerateVectorAsync(text, cancellationToken: ct);

        return TypedResults.Ok(new
        {
            dimensions = vector.Length,
            first5 = vector.Span[..5].ToArray()
        });
    });
}

app.UseHttpsRedirection();

app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = c => c.Tags.Contains("ready") });

app.MapApplicationEndpoints();
app.MapDocumentEndpoints();

app.Run();

public partial class Program;