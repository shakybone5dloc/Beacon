using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using Beacon.Contracts.Applications;

namespace Beacon.Tests.Integrations;

public sealed class ApplicationTests(BeaconApiFactory factory) : IClassFixture<BeaconApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Create_then_get_round_trips()
    {
        var ct = TestContext.Current.CancellationToken;
        var request = new CreateApplicationRequest { Company = "Contoso", Role = "Senior .NET Engineer" };

        var createResponse = await _client.PostAsJsonAsync("/api/applications", request, ct);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<ApplicationResponse>(ct);
        Assert.NotNull(created);
        Assert.Equal("Saved", created.Status);
        Assert.Equal(factory.Clock.GetUtcNow(), created.CreatedAt);
        Assert.Equal($"/api/applications/{created.Id}", createResponse.Headers.Location?.ToString());

        var fetched = await _client.GetFromJsonAsync<ApplicationResponse>(createResponse.Headers.Location, ct);
        Assert.Equal(created, fetched);
    }

    [Fact]
    public async Task Create_with_invalid_body_returns_400()
    {
        var ct = TestContext.Current.CancellationToken;
        var request = new CreateApplicationRequest { Company = "   ", Role = "" };

        var response = await _client.PostAsJsonAsync("/api/applications", request, ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_unknown_id_returns_404()
    {
        var ct = TestContext.Current.CancellationToken;
        var unknownId = Guid.NewGuid();
        var response = await _client.GetAsync($"/api/applications/{unknownId}", ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task List_returns_newest_first()
    {
        var ct = TestContext.Current.CancellationToken;

        var older = await CreateAsync("Older Co", ct);
        factory.Clock.Advance(TimeSpan.FromMinutes(1));
        var newer = await CreateAsync("Newer Co", ct);

        var list = await _client.GetFromJsonAsync<List<ApplicationResponse>>("/api/applications", ct);

        Assert.NotNull(list);
        var newerIndex = list.FindIndex(a => a.Id == newer.Id);
        var olderIndex = list.FindIndex(a => a.Id == older.Id);

        Assert.True(newerIndex >= 0 && olderIndex >= 0, "both applications should be in the list");
        Assert.True(newerIndex < olderIndex, "newer application should come before older");
    }

    private async Task<ApplicationResponse> CreateAsync(string company, CancellationToken ct)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/applications",
            new CreateApplicationRequest { Company = company, Role = "ngineer" },
            ct);

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ApplicationResponse>(ct))!;
    }
}