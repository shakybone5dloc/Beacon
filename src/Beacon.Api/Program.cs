using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using System.Threading.RateLimiting;
using Beacon.Infrastructure;
using Beacon.Api.Endpoints;
using Beacon.Application;
using Beacon.Contracts;
using Beacon.Api.Errors;
using Beacon.Api.RateLimiting;
using Beacon.Api.Health;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ConflictExceptionHandler>();
builder.Services.AddExceptionHandler<AiUnavailableExceptionHandler>();
builder.Services.AddContracts();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddOptions<AskRateLimitOptions>()
    .BindConfiguration(AskRateLimitOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.OnRejected = async (context, ct) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();
        }

        var problemDetails = context.HttpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
        await problemDetails.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = context.HttpContext,
            ProblemDetails =
            {
                Title = "Too many requests",
                Detail = "You've asked a lot of questions in a short time, Please wait and try again.",
                Status = StatusCodes.Status429TooManyRequests
            }
        });
    };

    options.AddPolicy(AskRateLimitOptions.PolicyName, http =>
    {
        var limits = http.RequestServices.GetRequiredService<IOptions<AskRateLimitOptions>>().Value;

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = limits.PermitLimit,
                Window = TimeSpan.FromSeconds(limits.WindowSeconds),
                QueueLimit = 0
            });
    });
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseRateLimiter();

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
    app.MapGet("/dev/chat", async (string prompt, IChatClient chat, HttpResponse response, CancellationToken ct) =>
    {
        response.ContentType = "text/plain; charset=utf-8";

        await foreach (var update in chat.GetStreamingResponseAsync(prompt, cancellationToken: ct))
        {
            await response.WriteAsync(update.Text, ct);
            await response.Body.FlushAsync(ct);
        }
    });
}

app.UseHttpsRedirection();

app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions 
{ 
    Predicate = c => c.Tags.Contains("ready"),
    ResponseWriter = HealthResponseWriter.WriteJson
});

app.MapApplicationEndpoints();
app.MapDocumentEndpoints();
app.MapSearchEndpoints();
app.MapAskEndpoints();

app.Run();

public partial class Program;