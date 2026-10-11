using System.Net;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Resumaire.Api.Ai;
using Resumaire.Api.Tailoring;

namespace Resumaire.Api.Configuration;

public static class AiServiceConfiguration
{
    public static IServiceCollection AddPersonalAiServices(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var protection = services.AddDataProtection().SetApplicationName("Resumaire");
        var path = configuration["DataProtection:KeyPath"];
        var certificatePath = configuration["DataProtection:CertificatePath"];
        if (environment.IsProduction() && (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(certificatePath)))
            throw new InvalidOperationException("Production requires DataProtection:KeyPath (persistent storage) and DataProtection:CertificatePath (a secret-mounted PFX with a private key).");
        if (!string.IsNullOrWhiteSpace(path)) protection.PersistKeysToFileSystem(new DirectoryInfo(path));
        if (!string.IsNullOrWhiteSpace(certificatePath))
        {
            var certificate = X509CertificateLoader.LoadPkcs12FromFile(certificatePath, configuration["DataProtection:CertificatePassword"], X509KeyStorageFlags.EphemeralKeySet);
            if (!certificate.HasPrivateKey) throw new InvalidOperationException("The Data Protection certificate must contain a private key.");
            protection.ProtectKeysWithCertificate(certificate);
        }

        var proxies = configuration.GetSection("Proxy:KnownProxies").Get<string[]>() ?? [];
        if (environment.IsProduction() && proxies.Length == 0)
            throw new InvalidOperationException("Production requires Proxy:KnownProxies containing the trusted reverse proxy IP address.");
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            foreach (var address in proxies)
            {
                if (!IPAddress.TryParse(address, out var ip)) throw new InvalidOperationException("Proxy:KnownProxies must contain IP addresses.");
                options.KnownProxies.Add(ip);
            }
        });
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<AiProviderRegistry>();
        services.AddSingleton<AiCredentialProtector>();
        services.AddScoped<AiSettingsService>();
        services.AddScoped<AiUsageService>();
        services.AddHttpClient<IAiProviderClient, AiProviderClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(Math.Clamp(configuration.GetValue<int?>("OpenAI:TimeoutSeconds") ?? 60, 5, 120));
            client.MaxResponseContentBufferSize = 2 * 1024 * 1024;
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false }).RemoveAllLoggers();
        services.AddScoped<IAiTailoringSuggestionGenerator, OpenAiTailoringSuggestionGenerator>();
        return services;
    }
}
