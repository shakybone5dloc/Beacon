using System.Net;
using System.Net.Http.Json;
using Beacon.Contracts.Search;
using Beacon.Tests.Fakes;

namespace Beacon.Tests.Integrations;

public sealed class SearchTests(BeaconApiFactory factory) : IClassFixture<BeaconApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Relevant_document_ranks_first()
    {
        var ct = TestContext.Current.CancellationToken;

        var relevant = await _client.UploadAndWaitAsync("devops.md",
            "Kubernetes clusters, Docker containers and Helm charts on Azure", ct: ct);
        var unrelated = await _client.UploadAndWaitAsync("baking.md",
            "Sourdough bread with rye flour baked in a cast iron oven", ct: ct);
        Assert.Equal("Ready", relevant.Status);
        Assert.Equal("Ready", unrelated.Status);

        var results = await _client.GetFromJsonAsync<List<SearchResult>>(
            "/api/search?q=kubernetes%20docker%20containers", ct);

        Assert.NotNull(results);
        Assert.NotEmpty(results);
        Assert.Equal(relevant.Id, results[0].DocumentId);
        Assert.True(results[0].Score > 0.3, $"Expected a strong match, got {results[0].Score}");
    }

    [Fact]
    public async Task ApplicationId_filter_excludes_other_applications()
    {
        var ct = TestContext.Current.CancellationToken;
        var fileName = "Resume.md";
        var text = "Terraform modules for infrastructure as code";

        var firstApp = await _client.CreateApplicationAsync(
            "CompanyOne", ct);
        var secondApp = await _client.CreateApplicationAsync(
            "CompanyTwo", ct);

        var firstDoc = await _client.UploadAndWaitAsync(fileName,
            text, applicationId: firstApp.Id, ct: ct);
        var secondDoc = await _client.UploadAndWaitAsync(fileName,
            text, applicationId: secondApp.Id, ct: ct);

        Assert.Equal("Ready", firstDoc.Status);
        Assert.Equal("Ready", secondDoc.Status);

        var results = await _client.GetFromJsonAsync<List<SearchResult>>(
            $"/api/search?q=terraform&applicationId={firstApp.Id}", ct);

        Assert.NotNull(results);
        Assert.All(results, r => Assert.Equal(firstDoc.Id, r.DocumentId));

    }

    [Fact]
    public async Task Empty_query_returns_400()
    {
        var ct = TestContext.Current.CancellationToken;

        var doc = await _client.UploadAndWaitAsync("resume.md",
            "Nothing here", ct: ct);

        Assert.Equal("Ready", doc.Status);

        var results = await _client.GetAsync("/api/search?q=", ct);

        Assert.Equal(HttpStatusCode.BadRequest, results.StatusCode);
    }

    [Fact]
    public async Task Embedding_failure_marks_document_failed()
    {
        var ct = TestContext.Current.CancellationToken;

        var doc = await _client.UploadAndWaitAsync("fail.md",
            $"Some text {FakeEmbeddingGenerator.FailMarker} more text", ct: ct);

        Assert.Equal("Failed", doc.Status);
        Assert.Equal("Embedding failed: Fake embedding failure", doc.Error);
    }
}