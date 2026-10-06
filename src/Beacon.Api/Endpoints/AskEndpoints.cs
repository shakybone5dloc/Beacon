using System.Net.ServerSentEvents;
using Beacon.Application.Ask;
using Beacon.Contracts.Ask;
using Beacon.Api.RateLimiting;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Beacon.Api.Endpoints;

public static class AskEndpoints
{
    public static IEndpointRouteBuilder MapAskEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/ask", Ask).WithTags("Ask")
            .RequireRateLimiting(AskRateLimitOptions.PolicyName);
        return app;
    }

    private static ServerSentEventsResult<object> Ask(AskRequest request, AskService service, CancellationToken ct)
    {
        var events = service.AskAsync(request.Question, request.ApplicationId, ct);
        return TypedResults.ServerSentEvents(ToSse(events));
    }

    private static async IAsyncEnumerable<SseItem<object>> ToSse(IAsyncEnumerable<AskEvent> events)
    {
        await foreach (var e in events)
        {
            var type = e switch { 
                SourcesEvent => "sources", 
                TokenEvent => "token", 
                ErrorEvent => "error",
                DoneEvent => "done", _ => "message" };
            yield return new SseItem<object>(e, type);
        }
    }
    
}