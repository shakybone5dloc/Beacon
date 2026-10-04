using Beacon.Application.Documents;

namespace Beacon.Tests.Unit;

public sealed class TextChunkerTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_or_whitespace_gives_no_chunks(string text)
    {
        Assert.Empty(TextChunker.Chunk(text));
    }

    [Theory]
    [InlineData(1, 1)]      // tiny text -> one chunk
    [InlineData(1000, 1)]   // exactly one chunk's worth -> still one
    [InlineData(1001, 2)]   // one char over -> a second chunk
    [InlineData(2500, 3)]   // the diagram above
    [InlineData(1900, 2)]   // second chunk lands exactly on the end, no third chunk
    public void Produces_expected_chunk_count(int length, int expected)
    {
        var text = new string('a', length);

        Assert.Equal(expected, TextChunker.Chunk(text).Count);
    }

    [Fact]
    public void Consecutive_chunks_overlap_by_100_characters()
    {
        // Distinct characters so a wrong offset can't accidentally pass
        var text = string.Concat(Enumerable.Range(0, 2500).Select(i => (char)('a' + i % 26)));

        var chunks = TextChunker.Chunk(text);

        Assert.Equal(chunks[0][^100..], chunks[1][..100]);


    }

    [Fact]
    public void Last_chunk_ends_exactly_at_end_of_text()
    {
        var text = string.Concat(Enumerable.Range(0, 2500).Select(i => (char)('a' + i % 26)));

        var chunks = TextChunker.Chunk(text);

        Assert.EndsWith(chunks[^1], text);
    }

    [Fact]
    public void Overlap_must_be_smaller_than_chunk_size()
    {
        Assert.Throws<ArgumentException>(() => TextChunker.Chunk("hello", chunkSize: 100, overlap: 100));
    }
}