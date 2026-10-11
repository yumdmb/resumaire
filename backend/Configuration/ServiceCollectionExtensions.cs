using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Resumaire.Api.Data;
using Resumaire.Api.Export;
using Resumaire.Api.Infrastructure.Health;
using Resumaire.Api.Tailoring;

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

        services
            .AddOptions<TypstOptions>()
            .Bind(configuration.GetSection(TypstOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.Executable), "Typst:Executable must not be empty.")
            .ValidateOnStart();

        services
            .AddOptions<OpenAiOptions>()
            .Bind(configuration.GetSection(OpenAiOptions.SectionName))
            .Validate(options => options.MaxOutputTokens is >= 1000 and <= 16000,
                "OpenAI:MaxOutputTokens must be between 1000 and 16000.")
            .ValidateOnStart();

        return services;
    }

    public static IServiceCollection AddResumaireDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        var provider = configuration.GetValue<string>("Database:Provider");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required.");
        }

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            if (string.Equals(provider, "Sqlite", StringComparison.OrdinalIgnoreCase))
            {
                options.UseSqlite(connectionString);
                return;
            }

            options.UseNpgsql(connectionString);
        });

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
        services.AddMemoryCache(options => options.SizeLimit = 50L * 1024 * 1024);
        services.AddSingleton<ITypstPdfRenderer, TypstPdfRenderer>();
        services.AddScoped<IJobKeywordExtractor, JobKeywordExtractor>();
        services.AddScoped<IResumeKeywordComparer, ResumeKeywordComparer>();
        services.AddScoped<TailoringSuggestionGuardrails>();

        services.AddAiTailoringRateLimiting(configuration);

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
