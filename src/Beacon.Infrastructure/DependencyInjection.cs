using Beacon.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Beacon.Application.Abstractions;

namespace Beacon.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var cs =configuration.GetConnectionString("Beacon" ?? throw new InvalidOperationException("Connection string 'Beacon' is not configured."));

        // -- Database --
        services.AddDbContext<BeaconDbContext>(o => o.UseNpgsql(
            configuration.GetConnectionString("Beacon")
                ?? throw new InvalidOperationException("Connection string 'Beacon' is not configured.")));

        services.AddScoped<IBeaconDbContext>(sp => sp.GetRequiredService<BeaconDbContext>());

        services.AddHealthChecks()
            .AddDbContextCheck<BeaconDbContext>("database", tags: ["ready"]);

        return services;
    }
}