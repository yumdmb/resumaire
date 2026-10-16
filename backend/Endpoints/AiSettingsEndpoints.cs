using Microsoft.EntityFrameworkCore;
using Resumaire.Api.Ai;
using Resumaire.Api.Infrastructure.Api;
using Resumaire.Api.Infrastructure.Auth;

namespace Resumaire.Api.Endpoints;

public static class AiSettingsEndpoints
{
    public static IEndpointRouteBuilder MapAiSettingsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/settings/ai").RequireAuthorization().RequireRateLimiting("ai-settings")
            .AddEndpointFilter(async (context, next) =>
            {
                context.HttpContext.Response.Headers.CacheControl = "no-store";
                try { return await next(context); }
                catch (AiConnectionException exception) { return Failure(context.HttpContext, exception); }
                catch (DbUpdateException)
                { return Results.Problem(statusCode: 409, title: "Settings changed", detail: "Your settings changed in another window. Reload and try again."); }
            });
        group.MapGet("/", async (AiSettingsService service, HttpContext context, CancellationToken ct) =>
            ApiResponses.Ok(await service.GetAsync(context.User.GetUserId(), ct)));
        group.MapPost("/models", async (AiConnectionRequest request, AiSettingsService service, HttpContext context, CancellationToken ct) =>
            ApiResponses.Ok(await service.DiscoverAsync(context.User.GetUserId(), request, ct)));
        group.MapPost("/test", async (AiConnectionRequest request, AiSettingsService service, HttpContext context, CancellationToken ct) =>
            ApiResponses.Ok(await service.TestAsync(context.User.GetUserId(), request, ct)));
        group.MapPut("/", async (AiConnectionRequest request, AiSettingsService service, HttpContext context, CancellationToken ct) =>
            ApiResponses.Ok(await service.SaveAsync(context.User.GetUserId(), request, ct)));
        group.MapDelete("/key", async (AiSettingsService service, HttpContext context, CancellationToken ct) =>
        {
            await service.RemoveAsync(context.User.GetUserId(), ct);
            return Results.NoContent();
        });
        return endpoints;
    }

    public static IResult Failure(HttpContext context, AiConnectionException exception)
    {
        if (exception.RetryAfter is { } retry) context.Response.Headers.RetryAfter = retry.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return Results.Problem(statusCode: exception.Status, title: "AI connection", detail: exception.Message,
            extensions: new Dictionary<string, object?> { ["code"] = exception.Code });
    }
}
