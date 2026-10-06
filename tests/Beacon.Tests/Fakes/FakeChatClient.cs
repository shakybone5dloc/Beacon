using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace Beacon.Tests.Fakes;

public sealed class FakeChatClient : IChatClient
{
    public const string CannedAnswer = "You have Kubernetes experience [1].";

    private int _calls;
    public int Calls => Volatile.Read(ref _calls);
    public IReadOnlyList<ChatMessage>? LastMessages { get; private set; }

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        Record(messages);
        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, CannedAnswer)));
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Record(messages);

        var words = CannedAnswer.Split(' ');
        for (var i = 0; i < words.Length; i++)
        {
            await Task.Yield();
            var piece = i == 0 ? words[i] : " " + words[i];
            yield return new ChatResponseUpdate(ChatRole.Assistant, piece);
        }
    }

    private void Record(IEnumerable<ChatMessage> messages)
    {
        Interlocked.Increment(ref _calls);
        LastMessages = messages.ToList();
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;
    public void Dispose() { }
}