using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Resumaire.Api.Data;
using Resumaire.Api.Infrastructure.Health;

namespace Resumaire.Api.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddResumaireOptions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<CorsOptions>()
            .Bind(configuration.GetSection(CorsOptions.SectionName))
            .Validate(options => options.AllowedOrigins.Length > 0, "At least one CORS origin must be configured.")
            .ValidateOnStart();

        return services;
    }

    public static IServiceCollection AddResumaireDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required.");
        }

        services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));

        return services;
    }

    public static IServiceCollection AddResumaireIdentity(this IServiceCollection services)
    {
        services
            .AddIdentityApiEndpoints<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 12;
                options.Password.RequireNonAlphanumeric = false;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>();

        services.AddAuthorization();

        return services;
    }

    public static IServiceCollection AddResumaireApiServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var corsOptions = configuration
            .GetSection(CorsOptions.SectionName)
            .Get<CorsOptions>()
            ?? new CorsOptions();

        services.AddProblemDetails();
        services.AddOpenApi();

        services.AddCors(options =>
        {
            options.AddPolicy(CorsOptions.SectionName, policy =>
            {
                policy
                    .WithOrigins(corsOptions.AllowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        services
            .AddHealthChecks()
            .AddCheck<ApplicationDbHealthCheck>("database");

        return services;
    }
}
