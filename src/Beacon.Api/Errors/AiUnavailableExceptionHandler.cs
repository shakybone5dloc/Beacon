using Microsoft.AspNetCore.Diagnostics;
using Polly;

namespace Beacon.Api.Errors;

internal sealed class AiUnavailableExceptionHandler(
    IProblemDetailsService problemDetails,
    ILogger<AiUnavailableExceptionHandler> logger) : IExceptionHandler
{
    private const int RetryAfterSeconds = 30;

    public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception exception, CancellationToken ct)
    {
        if (!IsAiUnavailable(exception))
            return false;

        logger.LogWarning(exception, "Ai service unavailable while handling {Path}", http.Request.Path);

        http.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;

        http.Response.Headers.RetryAfter = RetryAfterSeconds.ToString();

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = http,
            Exception = exception,
            ProblemDetails =
            {
                Title = "Ai service unavailable",
                Detail = "The Ai service is temporarily unavailable. Please try again shortly.",
                Status = StatusCodes.Status503ServiceUnavailable
            }
        });
    }

    private static bool IsAiUnavailable(Exception exception)
    {
        for (var e = exception; e is not null; e = e.InnerException)
        {
            if (e is HttpRequestException or ExecutionRejectedException) return true;
        }
        return false;
    }
}