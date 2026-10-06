using System.Net;
using System.Text.Json;

namespace Beacon.Tests.Integrations;

public sealed class HealthTests(BeaconApiFactory factory) : IClassFixture<BeaconApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task HealthCheck_Returns200()
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await _client.GetAsync("/health/live", ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Ready_stays_200_but_reports_ai_degraded_when_ollama_is_down()
    {
        var ct = TestContext.Current.CancellationToken;

        var response = await _client.GetAsync("/health/ready", ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        var root = json.RootElement;

        Assert.Equal("Degraded", root.GetProperty("status").GetString());
        Assert.Equal("Healthy", root.GetProperty("checks").GetProperty("database").GetProperty("status").GetString());
        Assert.Equal("Degraded", root.GetProperty("checks").GetProperty("ollama").GetProperty("status").GetString());
    }
}