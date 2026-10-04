using System.Threading.Channels;
using Beacon.Application.Abstractions;

namespace Beacon.Infrastructure.Documents;

internal sealed class ChannelDocumentQueue : IDocumentQueue
{
    private readonly Channel<Guid> _channel =
        Channel.CreateUnbounded<Guid>(new UnboundedChannelOptions { SingleReader = true });

    public ValueTask EnqueueAsync(Guid documentId, CancellationToken ct = default) =>
        _channel.Writer.WriteAsync(documentId, ct);

    public IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken ct) =>
        _channel.Reader.ReadAllAsync(ct);
}