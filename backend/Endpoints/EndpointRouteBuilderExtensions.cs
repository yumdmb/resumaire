using Resumaire.Api.Data;
using Resumaire.Api.Infrastructure.Api;
using Resumaire.Api.Infrastructure.Auth;

namespace Resumaire.Api.Endpoints;

public static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapResumaireEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/", (IHostEnvironment environment) =>
            ApiResponses.Ok(new ApiStatus(
                Name: "Resumaire API",
                Environment: environment.EnvironmentName,
                OpenApiDocument: environment.IsDevelopment() ? "/openapi/v1.json" : null)))
            .WithName("GetApiStatus");

        endpoints.MapHealthChecks("/health").AllowAnonymous();

        endpoints
            .MapGroup("/api/auth")
            .MapIdentityApi<ApplicationUser>();

        endpoints.MapGet("/api/users/me", (HttpContext httpContext) =>
            ApiResponses.Ok(new CurrentUserResponse(httpContext.User.GetUserId())))
            .RequireAuthorization()
            .WithName("GetCurrentUser");

        return endpoints;
    }
}

internal sealed record ApiStatus(string Name, string Environment, string? OpenApiDocument);

internal sealed record CurrentUserResponse(string UserId);
