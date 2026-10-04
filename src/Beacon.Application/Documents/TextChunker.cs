namespace Beacon.Application.Documents;

public static class TextChunker
{
    public static IReadOnlyList<string> Chunk(string text, int chunkSize = 1000, int overlap = 100)
    {
        // Guard clauses: bad settings are a programming error, so fail loudly
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(chunkSize);
        ArgumentOutOfRangeException.ThrowIfNegative(overlap);
        if (overlap >= chunkSize)
            throw new ArgumentException("Overlap must be smaller than chunk size.", nameof(overlap));

        if (string.IsNullOrWhiteSpace(text)) return [];

        var chunks = new List<string>();
        var step = chunkSize - overlap;

        for (var start = 0; start < text.Length; start += step)
        {
            var length = Math.Min(chunkSize, text.Length - start);
            chunks.Add(text.Substring(start, length));

            if (start + length >= text.Length)
                break;
        }

        return chunks;
    }
}