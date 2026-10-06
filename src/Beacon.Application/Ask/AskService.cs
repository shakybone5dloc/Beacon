using System.Runtime.CompilerServices;
using Beacon.Application.Search;
using Beacon.Contracts.Ask;
using Beacon.Contracts.Search;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.AI;

namespace Beacon.Application.Ask;

public sealed class AskService(SearchService search, IChatClient chat, ILogger<AskService> logger)
{
    public const int SourceCount = 6;
    public const double MinScore = 0.3;
    public const string NoSourceAnswer = "I couldn't find anything in your documents about that.";
    public const string AiUnavailableMessage = "The AI service is temporarily unavailable. Please try again shortly.";

    public async IAsyncEnumerable<AskEvent> AskAsync(
        string question,
        Guid? applicationId,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        IReadOnlyList<SearchResult> results = [];
        var retrievalFailed = false;
        try
        {
           results = await search.SearchAsync(question, SourceCount, applicationId, kind: null, ct);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Retrieval failed for ask");
            retrievalFailed = true;
        }

        if (retrievalFailed)
        {
            yield return new ErrorEvent(AiUnavailableMessage);
            yield return new DoneEvent(Grounded: false);
            yield break;
        }

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

        await using var updates = chat.GetStreamingResponseAsync(messages, cancellationToken: ct).GetAsyncEnumerator(ct);
        var generationFailed = false;

        while (true)
        {
            ChatResponseUpdate update;
            try
            {
                if (!await updates.MoveNextAsync()) break;
                update = updates.Current;
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                logger.LogWarning(ex, "Chat stream failed mid-answer");
                generationFailed = true;
                break;
            }

            if (!string.IsNullOrEmpty(update.Text))
                yield return new TokenEvent(update.Text);
        }

        if (generationFailed)
        {
            yield return new ErrorEvent(AiUnavailableMessage);
            yield return new DoneEvent(Grounded: false);
            yield break;
        }

        yield return new DoneEvent(Grounded: true);
    }
}