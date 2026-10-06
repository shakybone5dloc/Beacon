using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Net.Http;
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
            .AddDbContextCheck<BeaconDbContext>("database", tags: ["ready"])
            .AddCheck<OllamaHealthCheck>("ollama", failureStatus: HealthStatus.Degraded, tags: ["ready"]);

        services.AddSingleton<ChannelDocumentQueue>();
        services.AddSingleton<IDocumentQueue>(sp => sp.GetRequiredService<ChannelDocumentQueue>());
        services.AddHostedService<DocumentProcessingWorker>();

        services.AddOptions<AiOptions>()
            .BindConfiguration(AiOptions.SectionName)
            .ValidateDataAnnotations()
            .Validate(o => o.EmbeddingDimensions == DocumentChunkConfiguration.EmbeddingDimensions,
            $"Ai:EmbeddingDimensions must be {DocumentChunkConfiguration.EmbeddingDimensions} to match the database column.")
            .ValidateOnStart();

        services.AddHttpClient(AiHttpClients.Embeddings, ConfigureOllamaClient)
            .AddStandardResilienceHandler(o =>
            {
                o.AttemptTimeout.Timeout = TimeSpan.FromSeconds(30);
                o.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(90);
                o.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(60);
            });

        services.AddHttpClient(AiHttpClients.Chat, ConfigureOllamaClient)
            .AddStandardResilienceHandler(o =>
            {
                o.AttemptTimeout.Timeout = TimeSpan.FromMinutes(2);
                o.TotalRequestTimeout.Timeout = TimeSpan.FromMinutes(3);
                o.CircuitBreaker.SamplingDuration = TimeSpan.FromMinutes(4);
            });

        services.AddHttpClient(AiHttpClients.Health, (sp, http) =>
        {
            ConfigureOllamaClient(sp, http);
            http.Timeout = TimeSpan.FromSeconds(3);
        });

        services.AddScoped<IEmbeddingGenerator<string, Embedding<float>>>(sp =>
        {
            var ai = sp.GetRequiredService<IOptions<AiOptions>>().Value;
            var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient(AiHttpClients.Embeddings);
            return new OllamaApiClient(http, ai.EmbeddingModel);
        });

        services.AddChatClient(sp =>
        {
            var ai = sp.GetRequiredService<IOptions<AiOptions>>().Value;
            var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient(AiHttpClients.Chat);
            return new OllamaApiClient(http, ai.ChatModel);
        }, ServiceLifetime.Scoped);

        return services;
    }

    private static void ConfigureOllamaClient(IServiceProvider sp, HttpClient http)
    {
        var ai = sp.GetRequiredService<IOptions<AiOptions>>().Value;
        http.BaseAddress = new Uri(ai.Endpoint);
        http.Timeout = Timeout.InfiniteTimeSpan;
    }
}