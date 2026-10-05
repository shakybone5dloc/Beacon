using Microsoft.Extensions.AI;

namespace Beacon.Tests.Fakes;

// A deterministic stand-in for Ollama: same text -> same vector, every time, no network.
// Each word bumps one of 768 slots, so texts that share words point in similar directions.
// That's crude, but ut's enough to test ranking and filtering for real.
public sealed class FakeEmbeddingGenerator : IEmbeddingGenerator<string, Embedding<float>>
{
    public const string FailMarker = "FAIL_EMBEDDING";
    private const int Dimensions = 768;
    private static readonly char[] Seperators = [' ', '\n', '\r', '\t', '.', ',', ';', ':', '!', '?', '(', ')'];

    public Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var texts = values.ToList();

        if (texts.Any(t => t.Contains(FailMarker)))
            throw new InvalidOperationException("Fake embedding failure");

        var embeddings = texts.Select(t => new Embedding<float>(Embed(t)));
        return Task.FromResult(new GeneratedEmbeddings<Embedding<float>>(embeddings));
    }

    private static float[] Embed(string text)
    {
        var vector = new float[Dimensions];

        foreach (var word in text.ToLowerInvariant().Split(Seperators, StringSplitOptions.RemoveEmptyEntries))
            vector[StableHash(word) % Dimensions] += 1f;

        var length = MathF.Sqrt(vector.Sum(x => x * x));
        if (length == 0)
        {
            vector[0] = 1f;
            return vector;
        }

        for (var i = 0; i < vector.Length; i++)
            vector[i] /= length;

        return vector;
    }

    private static int StableHash(string s)
    {
        unchecked
        {
            var hash = 17;
            foreach (var c in s) hash = hash * 32 + c;
            return hash & int.MaxValue;
        }
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;
    public void Dispose() { }
}