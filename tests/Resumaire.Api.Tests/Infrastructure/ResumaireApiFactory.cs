using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Resumaire.Api.Data;

namespace Resumaire.Api.Tests.Infrastructure;

public sealed class ResumaireApiFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    public ResumaireApiFactory()
        : this("Host=localhost;Port=5432;Database=resumaire_tests;Username=postgres;Password=postgres")
    {
    }

    private ResumaireApiFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    public ResumaireApiFactory WithDatabase(string databaseConnectionString) => new(databaseConnectionString);

    public async Task MigrateDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await dbContext.Database.MigrateAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");
        builder.UseSetting("ConnectionStrings:DefaultConnection", _connectionString);
        builder.UseSetting("Cors:AllowedOrigins:0", "http://localhost:5173");

        builder.ConfigureTestServices(services =>
        {
            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.AuthenticationScheme;
                    options.DefaultChallengeScheme = TestAuthHandler.AuthenticationScheme;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.AuthenticationScheme,
                    _ => { });
        });
    }
}
