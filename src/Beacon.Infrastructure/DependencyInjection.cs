using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Beacon.Application.Abstractions;
using Beacon.Infrastructure.Documents;
using Beacon.Infrastructure.Ai;
using Beacon.Infrastructure.Data;
using Beacon.Infrastructure.Data.Configurations;
using OllamaSharp;

namespace Beacon.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // -- Database --
        services.AddDbContext<BeaconDbContext>(o => o.UseNpgsql(
            configuration.GetConnectionString("Beacon")
                ?? throw new InvalidOperationException("Connection string 'Beacon' is not configured."),
            npgsql => npgsql.UseVector()));


        services.AddScoped<IBeaconDbContext>(sp => sp.GetRequiredService<BeaconDbContext>());
        services.AddScoped<IVectorStore, PgVectorStore>();

        services.AddHealthChecks()
            .AddDbContextCheck<BeaconDbContext>("database", tags: ["ready"]);

        services.AddSingleton<ChannelDocumentQueue>();
        services.AddSingleton<IDocumentQueue>(sp => sp.GetRequiredService<ChannelDocumentQueue>());
        services.AddHostedService<DocumentProcessingWorker>();

        services.AddOptions<AiOptions>()
            .BindConfiguration(AiOptions.SectionName)
            .ValidateDataAnnotations()
            .Validate(o => o.EmbeddingDimensions == DocumentChunkConfiguration.EmbeddingDimensions,
            $"Ai:EmbeddingDimensions must be {DocumentChunkConfiguration.EmbeddingDimensions} to match the database column.")
            .ValidateOnStart();

        services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(sp =>
        {
            var ai = sp.GetRequiredService<IOptions<AiOptions>>().Value;
            return new OllamaApiClient(new Uri(ai.Endpoint), ai.EmbeddingModel);
        });

        return services;
    }
}