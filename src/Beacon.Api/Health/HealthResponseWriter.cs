using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Beacon.Api.Health;

internal static class HealthResponseWriter
{
    public static Task WriteJson(HttpContext http, HealthReport report)
    {
        http.Response.ContentType = "application/json";
        return http.Response.WriteAsJsonAsync(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.ToDictionary(
                e => e.Key,
                e => new
                {
                    status = e.Value.Status.ToString(),
                    description = e.Value.Description,
                    durationMs = Math.Round(e.Value.Duration.TotalMilliseconds, 1)
                })
        });
    }
}