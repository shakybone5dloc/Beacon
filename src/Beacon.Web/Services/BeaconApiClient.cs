using System.Net;
using Beacon.Contracts.Applications;
using Microsoft.AspNetCore.Mvc;

namespace Beacon.Web.Services;

public sealed class BeaconApiClient(HttpClient http)
{
    public async Task<IReadOnlyList<ApplicationResponse>> GetApplicationsAsync(CancellationToken ct = default)
    {
        return await http.GetFromJsonAsync<List<ApplicationResponse>>("api/applications", ct) ?? [];
    }

    public async Task<MoveResult> ChangeStatusAsync(Guid id, string status, uint version, CancellationToken ct = default)
    {
        var response = await http.PatchAsJsonAsync(
            $"api/applications/{id}/status",
            new ChangeStatusRequest { Status = status, Version = version },
            ct);

        switch (response.StatusCode)
        {
            case HttpStatusCode.OK:
                var updated = await response.Content.ReadFromJsonAsync<ApplicationResponse>(ct);
                return new MoveResult(MoveOutcome.Moved, updated);

            case HttpStatusCode.Conflict:
                var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(ct);
                return new MoveResult(MoveOutcome.Conflict, Message: problem?.Detail);

            case HttpStatusCode.NotFound:
                return new MoveResult(MoveOutcome.NotFound, Message: "This application no longer exists.");

            default:
                response.EnsureSuccessStatusCode();
                throw new InvalidOperationException($"Unexpected status {response.StatusCode}");
        }
    }

    public async Task<CreateResult> CreateApplicationAsync(CreateApplicationRequest request, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("api/applications", request, ct);

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(ct);
            return new CreateResult(null, problem?.Errors ?? new Dictionary<string, string[]>());
        }

        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<ApplicationResponse>(ct);
        
        return new CreateResult(created, null);
    }
}