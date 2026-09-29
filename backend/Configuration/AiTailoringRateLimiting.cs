using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace Resumaire.Api.Configuration;

public static class AiTailoringRateLimiting
{
    private const string SuggestionsPathSegment = "/tailoring/suggestions";

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

            // Only POST .../tailoring/suggestions is limited; it is the call that costs money.
            options.GlobalLimiter = PartitionedRateLimiter.CreateChained(
                PartitionedRateLimiter.Create<HttpContext, string>(context =>
                    CreatePartition(context, "minute", limits => limits.RequestsPerMinute, TimeSpan.FromMinutes(1))),
                PartitionedRateLimiter.Create<HttpContext, string>(context =>
                    CreatePartition(context, "day", limits => limits.RequestsPerDay, TimeSpan.FromDays(1))));

            options.OnRejected = async (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(System.Globalization.CultureInfo.InvariantCulture);
                }

                await Results.Problem(
                        statusCode: StatusCodes.Status429TooManyRequests,
                        title: "Too many AI suggestion requests",
                        detail: "You have reached the AI suggestion limit. Try again later.")
                    .ExecuteAsync(context.HttpContext);
            };
        });

        return services;
    }

    private static RateLimitPartition<string> CreatePartition(
        HttpContext context,
        string window,
        Func<AiTailoringLimitOptions, int> permitLimit,
        TimeSpan period)
    {
        if (!HttpMethods.IsPost(context.Request.Method) ||
            !context.Request.Path.Value!.EndsWith(SuggestionsPathSegment, StringComparison.OrdinalIgnoreCase))
        {
            return RateLimitPartition.GetNoLimiter($"none:{window}");
        }

        var limits = context.RequestServices.GetRequiredService<IOptions<AiTailoringLimitOptions>>().Value;
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.Connection.RemoteIpAddress?.ToString()
            ?? "anonymous";

        return RateLimitPartition.GetFixedWindowLimiter(
            $"{window}:{userId}",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit(limits),
                Window = period,
                QueueLimit = 0,
                AutoReplenishment = true
            });
    }
}
