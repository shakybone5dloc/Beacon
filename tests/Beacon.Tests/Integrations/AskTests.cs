using System.Net;
using System.Net.Http.Json;
using System.Net.ServerSentEvents;
using System.Text.Json;
using Beacon.Application.Ask;
using Beacon.Contracts.Ask;
using Beacon.Tests.Fakes;
using Microsoft.Extensions.AI;

namespace Beacon.Tests.Integrations;

public sealed class AskTests(BeaconApiFactory factory) : IClassFixture<BeaconApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Ground_answer_streams_sources_then_tokens_then_done()
    {
        var ct = TestContext.Current.CancellationToken;
        var doc = await _client.UploadAndWaitAsync("devops.md",
            "Kubernetes clusters, Docker containers and Helm charts on Azure", ct: ct);
        var callsBefore = factory.Chat.Calls;

        var events = await AskAsync("kubernetes docker containers", ct);

        Assert.Equal("sources", events[0].EventType);
        Assert.Equal("done", events[^1].EventType);
        Assert.All(events[1..^1], e => Assert.Equal("token", e.EventType));

        var sources = Parse<SourcesEvent>(events[0]).Sources;
        Assert.Contains(sources, s => s.DocumentId == doc.Id);
        Assert.Equal(1, sources[0].Number);

        var answer = string.Concat(events[1..^1].Select(e => Parse<TokenEvent>(e).Text));
        Assert.Equal(FakeChatClient.CannedAnswer, answer);

        Assert.True(Parse<DoneEvent>(events[^1]).Grounded);
        Assert.Equal(callsBefore + 1, factory.Chat.Calls);
    }

    [Fact]
    public async Task No_relevant_sources_never_calls_the_model()
    {
        var ct = TestContext.Current.CancellationToken;
        var callsBefore = factory.Chat.Calls;

        var events = await AskAsync("lasagna ricotta oregano", ct);

        Assert.Equal(3, events.Count);

        var sources = Parse<SourcesEvent>(events[0]).Sources;
        Assert.Empty(sources);

        var answer = string.Concat(events[1..^1].Select(e => Parse<TokenEvent>(e).Text));
        Assert.Equal(AskService.NoSourceAnswer, answer);

        Assert.False(Parse<DoneEvent>(events[^1]).Grounded);
        Assert.Equal(callsBefore, factory.Chat.Calls);
    }

    [Fact]
    public async Task Empty_question_returns_400()
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await _client.PostAsJsonAsync("/api/ask", new AskRequest { Question = "" }, ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Model_receives_the_retrieved_passage_in_the_user_message()
    {
        var ct = TestContext.Current.CancellationToken;
        await _client.UploadAndWaitAsync("terraform.md", "Terraform modules provision Azure networking", ct: ct);

        await AskAsync("terraform modules azure", ct);

        var messages = factory.Chat.LastMessages;
        Assert.NotNull(messages);

        Assert.Equal(ChatRole.System, messages[0].Role);
        Assert.Contains("Terraform modules provision Azure networking", messages[1].Text);
        Assert.DoesNotContain("terraform modules azure", messages[0].Text);
    }

    private async Task<List<SseItem<string>>> AskAsync(string question, CancellationToken ct)
    {
        var response = await _client.PostAsJsonAsync("/api/ask", new AskRequest { Question = question }, ct);
        response.EnsureSuccessStatusCode();
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        var events = new List<SseItem<string>>();
        await foreach (var item in SseParser.Create(stream).EnumerateAsync(ct))
            events.Add(item);
        return events;
    }

    private static T Parse<T>(SseItem<string> item) =>
        JsonSerializer.Deserialize<T>(item.Data, JsonSerializerOptions.Web)!;
}