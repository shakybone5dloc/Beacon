using System.Threading.RateLimiting;
using Microsoft.Extensions.Options;

namespace Beacon.Api.RateLimiting;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddBeaconRateLimiting(this IServiceCollection services)
    {
        services.AddOptions<AskRateLimitOptions>()
            .BindConfiguration(AskRateLimitOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = async (context, ct) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();
                }

                var problemDetails = context.HttpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
                await problemDetails.WriteAsync(new ProblemDetailsContext
                {
                    HttpContext = context.HttpContext,
                    ProblemDetails =
                    {
                        Title = "Too many requests",
                        Detail = "You've asked a lot of questions in a short time. Please wait and try again.",
                        Status = StatusCodes.Status429TooManyRequests
                    }
                });
            };

            options.AddPolicy(AskRateLimitOptions.PolicyName, http =>
            {
                var limits = http.RequestServices.GetRequiredService<IOptions<AskRateLimitOptions>>().Value;

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = limits.PermitLimit,
                        Window = TimeSpan.FromSeconds(limits.WindowSeconds),
                        QueueLimit = 0
                    });
            });
        });

        return services;
    }
}