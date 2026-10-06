using Microsoft.Extensions.AI;

namespace Beacon.Api.Endpoints;

public static class DevEndpoints
{
    public static IEndpointRouteBuilder MapDevEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/dev").WithTags("Dev");

        group.MapGet("/embed", async (
            string text,
            IEmbeddingGenerator<string, Embedding<float>> generator,
            CancellationToken ct) =>
        {
            ReadOnlyMemory<float> vector = await generator.GenerateVectorAsync(text, cancellationToken: ct);
            return TypedResults.Ok(new { dimensions = vector.Length, first5 = vector.Span[..5].ToArray() });
        });

        group.MapGet("/chat", async (string prompt, IChatClient chat, HttpResponse response, CancellationToken ct) =>
        {
            response.ContentType = "text/plain; charset=utf-8";
            await foreach (var update in chat.GetStreamingResponseAsync(prompt, cancellationToken: ct))
            {
                await response.WriteAsync(update.Text, ct);
                await response.Body.FlushAsync(ct);
            }
        });

        return app;
    }
}