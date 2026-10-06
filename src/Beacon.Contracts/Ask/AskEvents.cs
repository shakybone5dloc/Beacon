namespace Beacon.Contracts.Ask;

public sealed record AskSource(int Number, Guid DocumentId, string FileName, int ChunkIndex, string Text, double Score);

public abstract record AskEvent;
public sealed record SourcesEvent(IReadOnlyList<AskSource> Sources) : AskEvent;
public sealed record TokenEvent(string Text) : AskEvent;
public sealed record DoneEvent(bool Grounded) : AskEvent;
public sealed record ErrorEvent(string Message) : AskEvent;