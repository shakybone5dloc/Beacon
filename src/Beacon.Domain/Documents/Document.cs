namespace Beacon.Domain.Documents;

public class Document
{
    private readonly List<DocumentChunk> _chunks = [];

    private Document() { }

    public static Document Create(string fileName, DocumentKind kind, string content, Guid? jobApplicationId, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentNullException.ThrowIfNull(content);

        return new Document
        {
            Id = Guid.CreateVersion7(),
            FileName = fileName.Trim(),
            Kind = kind,
            Content = content,
            JobApplicationId = jobApplicationId,
            Status = DocumentStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void StartProcessing(DateTimeOffset now)
    {
        if (Status is not (DocumentStatus.Pending or DocumentStatus.Processing))
            throw new DomainException($"Cannot start processing a document that is {Status}.");

        Status = DocumentStatus.Processing;
        Error = null;
        UpdatedAt = now;
    }

    public void MarkReady(IReadOnlyList<string> chunkTexts, DateTimeOffset now)
    {
        if (Status is not DocumentStatus.Processing)
            throw new DomainException($"Cannot set ready from status: {Status}.");

        _chunks.Clear();
        for (var i = 0; i < chunkTexts.Count; i++)
            _chunks.Add(new DocumentChunk(Id, i, chunkTexts[i]));

        Status = DocumentStatus.Ready;
        UpdatedAt = now;
    }

    public void MarkFailed(string error, DateTimeOffset now)
    {
        if (Status is not DocumentStatus.Processing)
            throw new DomainException($"Cannot set Failed status from status: {Status}");
        Status = DocumentStatus.Failed;
        Error = error;
        UpdatedAt = now;
    }


    public Guid Id { get; private set; }
    public string FileName { get; private set; } = null!;
    public DocumentKind Kind { get; private set; }
    public DocumentStatus Status { get; private set; }
    public string Content { get; private set; } = null!;
    public string? Error { get; private set; }
    public Guid? JobApplicationId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    // Read-only to the outside world; only MarkReady can change it
    public IReadOnlyList<DocumentChunk> Chunks => _chunks;
}