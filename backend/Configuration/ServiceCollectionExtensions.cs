using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Resumaire.Api.Data;
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
            .AddOptions<OpenAiOptions>()
            .Bind(configuration.GetSection(OpenAiOptions.SectionName))
            .Validate(
                options => TryCreateAbsoluteHttpUri(NormalizeBaseUrl(options.BaseUrl), out _),
                "OpenAI:BaseUrl must be an absolute HTTP or HTTPS URL.")
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
        services.AddScoped<IJobKeywordExtractor, JobKeywordExtractor>();
        services.AddScoped<IResumeKeywordComparer, ResumeKeywordComparer>();
        services.AddScoped<TailoringSuggestionGuardrails>();
        services.AddHttpClient<IAiTailoringSuggestionGenerator, OpenAiTailoringSuggestionGenerator>(client =>
        {
            var options = configuration
                .GetSection(OpenAiOptions.SectionName)
                .Get<OpenAiOptions>()
                ?? new OpenAiOptions();

            client.BaseAddress = CreateBaseUri(options.BaseUrl);
        });

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

    private static Uri CreateBaseUri(string baseUrl)
    {
        var normalizedBaseUrl = NormalizeBaseUrl(baseUrl);
        if (!TryCreateAbsoluteHttpUri(normalizedBaseUrl, out var uri))
        {
            throw new InvalidOperationException("OpenAI:BaseUrl must be an absolute HTTP or HTTPS URL.");
        }

        return uri;
    }

    private static string NormalizeBaseUrl(string? baseUrl)
    {
        var value = string.IsNullOrWhiteSpace(baseUrl)
            ? new OpenAiOptions().BaseUrl
            : baseUrl.Trim();

        return value.EndsWith("/", StringComparison.Ordinal)
            ? value
            : $"{value}/";
    }

    private static bool TryCreateAbsoluteHttpUri(string value, out Uri uri)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var createdUri) ||
            (createdUri.Scheme != Uri.UriSchemeHttp && createdUri.Scheme != Uri.UriSchemeHttps))
        {
            uri = null!;
            return false;
        }

        uri = createdUri;
        return true;
    }
}
