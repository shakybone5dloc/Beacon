using System.Net.Http.Json;
using Beacon.Contracts.Applications;

namespace Beacon.Web.Services;

public sealed class BeaconApiClient(HttpClient http)
{
    public async Task<IReadOnlyList<ApplicationResponse>> GetApplicationsAsync(CancellationToken ct = default)
    {
        return await http.GetFromJsonAsync<List<ApplicationResponse>>("api/applications", ct) ?? [];
    }
}