using System.Net;
using System.Net.Http.Json;
using Beacon.Contracts.Ask;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace Beacon.Tests.Integrations;

public sealed class LowRateLimitApiFactory : BeaconApiFactory
{
    public const int Limit = 2;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting("RateLimiting:Ask:PermitLimit", Limit.ToString());
        builder.UseSetting("RateLimiting:Ask:WindowSeconds", "3600");
    }
}

public sealed class RateLimitTests(LowRateLimitApiFactory factory) : IClassFixture<LowRateLimitApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Requests_over_the_limit_get_429_with_retry_after()
    {
        var ct = TestContext.Current.CancellationToken;
        for (var i = 0; i < LowRateLimitApiFactory.Limit; i++)
        {
            var response = await _client.PostAsJsonAsync("/api/ask", new AskRequest { Question = $"question {i + 1}" }, ct);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        var rejected = await _client.PostAsJsonAsync("/api/ask", new AskRequest { Question = "one too many" }, ct);

        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        Assert.NotNull(rejected.Headers.RetryAfter);       

        var problem = await rejected.Content.ReadFromJsonAsync<ProblemDetails>(ct);
        Assert.NotNull(problem);
        Assert.Equal("Too many requests", problem.Title);
    }
}