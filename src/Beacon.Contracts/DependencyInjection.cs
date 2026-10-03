using Microsoft.Extensions.DependencyInjection;

namespace Beacon.Contracts;

public static class DependencyInjection
{
    public static IServiceCollection AddContracts(this IServiceCollection services)
    {
        services.AddValidation();
        return services;
    }
}