using Beacon.Infrastructure.Data;
using Beacon.Tests.Fakes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using Testcontainers.PostgreSql;

namespace Beacon.Tests.Integrations;

public class BeaconApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public static readonly DateTimeOffset StartTime = new(2026, 11, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder()
        .WithImage("pgvector/pgvector:pg17")
        .Build();

    public FakeTimeProvider Clock { get; } = new(StartTime);
    public FakeChatClient Chat { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.UseSetting("ConnectionStrings:Beacon", _db.GetConnectionString());
        builder.UseSetting("RateLimiting:Ask:PermitLimit", "1000");
        builder.UseSetting("Ai:Endpoint", "http://127.0.0.1:9");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);

            services.RemoveAll<IEmbeddingGenerator<string, Embedding<float>>>();
            services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>, FakeEmbeddingGenerator>();

            services.RemoveAll<IChatClient>();
            services.AddSingleton<IChatClient>(Chat);
        });
    }

    public async ValueTask InitializeAsync()
    {
        await _db.StartAsync();

        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<BeaconDbContext>();
        await context.Database.MigrateAsync();
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _db.DisposeAsync();
    }
}