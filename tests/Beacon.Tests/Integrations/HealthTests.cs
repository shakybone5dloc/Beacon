using System.Net;
using Beacon.Infrastructure.Data;

namespace Beacon.Tests.Integrations;

public sealed class HealthTests(BeaconApiFactory factory) : IClassFixture<BeaconApiFactory>
{
    [Fact]
    public async Task HealthCheck_Returns200()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = factory.CreateClient();
        var response = await client.GetAsync("/health/live", ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}