using Beacon.Application.Applications;
using Beacon.Application.Documents;
using Beacon.Application.Search;
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
        services.AddScoped<SearchService>();
        return services;
    }
}