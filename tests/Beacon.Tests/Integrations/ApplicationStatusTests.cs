using System.Net;
using System.Net.Http.Json;
using Beacon.Contracts.Applications;
using Microsoft.AspNetCore.Mvc;

namespace Beacon.Tests.Integrations;

public sealed class ApplicationStatusTests(BeaconApiFactory factory) : IClassFixture<BeaconApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Valid_move_returns_200_and_updates_status_timestamp_and_version()
    {
        var ct = TestContext.Current.CancellationToken;
        var created = await CreateAsync(ct);
        factory.Clock.Advance(TimeSpan.FromHours(1));

        var response = await PatchStatusAsync(created.Id, "Applied", created.Version, ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<ApplicationResponse>(ct);
        Assert.NotNull(updated);
        Assert.Equal("Applied", updated.Status);
        Assert.Equal(factory.Clock.GetUtcNow(), updated.UpdatedAt);
        Assert.Equal(created.CreatedAt, updated.CreatedAt);
        Assert.NotEqual(created.Version, updated.Version);
    }

    [Fact]
    public async Task Illegal_move_returns_409()
    {
        var ct = TestContext.Current.CancellationToken;
        var created = await CreateAsync(ct);

        var response = await PatchStatusAsync(created.Id, "Offer", created.Version, ct);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ProblemDetails>(ct);
        Assert.NotNull(body);
        Assert.Contains("Cannot move", body.Detail);
    }

    [Fact]
    public async Task Unknown_status_returns_400()
    {
        var ct = TestContext.Current.CancellationToken;
        var created = await CreateAsync(ct);

        var response = await PatchStatusAsync(created.Id, "Hired", created.Version, ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Stale_version_returns_409()
    {
        var ct = TestContext.Current.CancellationToken;
        var created = await CreateAsync(ct);

        var first = await PatchStatusAsync(created.Id, "Applied", created.Version, ct);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var second = await PatchStatusAsync(created.Id, "Interviewing", created.Version, ct);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        var body = await second.Content.ReadFromJsonAsync<ProblemDetails>(ct);
        Assert.NotNull(body);
        Assert.Contains("changed by someone else", body.Detail);
    }

    private async Task<ApplicationResponse> CreateAsync(CancellationToken ct)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/applications",
            new CreateApplicationRequest { Company = "Contoso", Role = "Engineer" },
            ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ApplicationResponse>(ct))!;
    }

    private Task<HttpResponseMessage> PatchStatusAsync(Guid id, string status, uint version, CancellationToken ct) =>
        _client.PatchAsJsonAsync(
            $"/api/applications/{id}/status",
            new ChangeStatusRequest { Status = status, Version = version },
            ct);
}