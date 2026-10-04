namespace Beacon.Domain.Documents;

public class DocumentChunk
{
    private DocumentChunk() { }

    internal DocumentChunk(Guid documentId, int index, string text)
    {
        Id = Guid.CreateVersion7();
        DocumentId = documentId;
        Index = index;
        Text = text;
    }

    public Guid Id { get; private set; }
    public Guid DocumentId { get; private set; }
    public int Index { get; private set; }
    public string Text { get; private set; } = null!;
}