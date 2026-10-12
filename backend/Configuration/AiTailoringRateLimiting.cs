using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Resumaire.Api.Configuration;

public static class AiTailoringRateLimiting
{
    public static IServiceCollection AddAiTailoringRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<AiTailoringLimitOptions>()
            .Bind(configuration.GetSection(AiTailoringLimitOptions.SectionName))
            .Validate(
                value => value.RequestsPerMinute > 0 && value.RequestsPerDay > 0,
                "AiTailoring limits must be positive.")
            .ValidateOnStart();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy("ai-settings", context => RateLimitPartition.GetFixedWindowLimiter(
                context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous-settings",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));

            options.OnRejected = async (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(System.Globalization.CultureInfo.InvariantCulture);
                }

                await Results.Problem(
                        statusCode: StatusCodes.Status429TooManyRequests,
                        title: "Too many AI settings requests",
                        detail: "You have reached the AI settings request limit. Try again later.")
                    .ExecuteAsync(context.HttpContext);
            };
        });

        return services;
    }

}
