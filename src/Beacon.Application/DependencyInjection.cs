using Beacon.Application.Applications;
using Beacon.Application.Documents;
using Microsoft.Extensions.DependencyInjection;

namespace Beacon.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<ApplicationService>();
        services.AddScoped<DocumentProcessingService>();
        services.AddScoped<DocumentService>();
        return services;
    }
}