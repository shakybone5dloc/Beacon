using System.Runtime.CompilerServices;
using Beacon.Application.Search;
using Beacon.Contracts.Ask;
using Microsoft.Extensions.AI;

namespace Beacon.Application.Ask;

public sealed class AskService(SearchService search, IChatClient chat)
{
    public const int SourceCount = 6;
    public const double MinScore = 0.3;
    public const string NoSourceAnswer = "I couldn't find anything in your documents about that.";

    public async IAsyncEnumerable<AskEvent> AskAsync(
        string question,
        Guid? applicationId,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var results = await search.SearchAsync(question, SourceCount, applicationId, kind: null, ct);

        var sources = results
            .Where(r => r.Score >= MinScore)
            .Select((r, i) => new AskSource(i + 1, r.DocumentId, r.FileName, r.ChunkIndex, r.Text, r.Score))
            .ToList();

        yield return new SourcesEvent(sources);

        if (sources.Count == 0)
        {
            yield return new TokenEvent(NoSourceAnswer);
            yield return new DoneEvent(Grounded: false);
            yield break;
        }

        var messages = PromptBuilder.Build(question, sources);

        await foreach (var update in chat.GetStreamingResponseAsync(messages, cancellationToken: ct))
        {
            if (!string.IsNullOrEmpty(update.Text))
                yield return new TokenEvent(update.Text);
        }

        yield return new DoneEvent(Grounded: true);
    }
}