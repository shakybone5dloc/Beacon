using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Beacon.Infrastructure.Ai;

internal sealed class OllamaHealthCheck(IHttpClientFactory httpClients) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            var http = httpClients.CreateClient(AiHttpClients.Health);
            using var response = await http.GetAsync("api/tags", ct);

            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy("Ollama is reachable")
                : new HealthCheckResult(context.Registration.FailureStatus,
                    $"Ollama returned {(int)response.StatusCode}");
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, "Ollama is unreachable", ex);
        }
    }
}