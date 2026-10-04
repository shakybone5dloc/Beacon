using Beacon.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Beacon.Application.Abstractions;
using Beacon.Infrastructure.Documents;
using Beacon.Infrastructure.Ai;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using OllamaSharp;

namespace Beacon.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // -- Database --
        services.AddDbContext<BeaconDbContext>(o => o.UseNpgsql(
            configuration.GetConnectionString("Beacon")
                ?? throw new InvalidOperationException("Connection string 'Beacon' is not configured.")));

        services.AddScoped<IBeaconDbContext>(sp => sp.GetRequiredService<BeaconDbContext>());

        services.AddHealthChecks()
            .AddDbContextCheck<BeaconDbContext>("database", tags: ["ready"]);

        services.AddSingleton<ChannelDocumentQueue>();
        services.AddSingleton<IDocumentQueue>(sp => sp.GetRequiredService<ChannelDocumentQueue>());
        services.AddHostedService<DocumentProcessingWorker>();

        services.AddOptions<AiOptions>()
            .BindConfiguration(AiOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(sp =>
        {
            var ai = sp.GetRequiredService<IOptions<AiOptions>>().Value;
            return new OllamaApiClient(new Uri(ai.Endpoint), ai.EmbeddingModel);
        });

        return services;
    }
}