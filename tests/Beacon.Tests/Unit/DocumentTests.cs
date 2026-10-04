using Beacon.Domain;
using Beacon.Domain.Documents;

namespace Beacon.Tests.Unit;

public sealed class DocumentTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 1, 12, 0, 0, TimeSpan.Zero);

    private static Document NewDocument() =>
        Document.Create("resume.md", DocumentKind.Resume, "some text", null, Now);

    [Fact]
    public void Happy_path_creates_numbered_chunks()
    {
        var doc = NewDocument();

        doc.StartProcessing(Now);
        doc.MarkReady(["First", "second", "third"], Now);

        Assert.Equal(DocumentStatus.Ready, doc.Status);
        Assert.Equal([0, 1, 2], doc.Chunks.Select(c => c.Index));
        Assert.All(doc.Chunks, c => Assert.Equal(doc.Id, c.DocumentId));
    }

    [Fact]
    public void Cannot_mark_ready_without_processing_first()
    {
        var doc = NewDocument();

        Assert.Throws<DomainException>(() => doc.MarkReady([], Now));
    }

    [Fact]
    public void Failure_records_the_error()
    {
        var doc = NewDocument();
        var error = "This Error";

        doc.StartProcessing(Now);
        doc.MarkFailed(error, Now);

        Assert.Equal(DocumentStatus.Failed, doc.Status);
        Assert.Equal(error, doc.Error);
    }

    [Fact]
    public void Retry_after_failure_is_not_allowed_from_Failed()
    {
        var doc = NewDocument();

        doc.StartProcessing(Now);
        doc.MarkFailed("error", Now);

        Assert.Throws<DomainException>(() => doc.StartProcessing(Now));
    }
}