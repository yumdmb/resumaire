using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Resumaire.Api.Data;
using System.Data.Common;

namespace Resumaire.Api.Tests.Infrastructure;

public sealed class ResumaireApiFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;
    private readonly DbConnection? _dbConnection;
    private readonly string? _databaseProvider;

    public ResumaireApiFactory()
        : this("Host=localhost;Port=5432;Database=resumaire_tests;Username=postgres;Password=postgres")
    {
    }

    private ResumaireApiFactory(
        string connectionString,
        DbConnection? dbConnection = null,
        string? databaseProvider = null)
    {
        _connectionString = connectionString;
        _dbConnection = dbConnection;
        _databaseProvider = databaseProvider;
    }

    public ResumaireApiFactory WithDatabase(string databaseConnectionString) => new(databaseConnectionString);

    public ResumaireApiFactory WithSqlite()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        return new ResumaireApiFactory("Data Source=:memory:", connection, "Sqlite");
    }

    public async Task MigrateDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        if (_dbConnection is not null)
        {
            await dbContext.Database.EnsureCreatedAsync();
            return;
        }

        await dbContext.Database.MigrateAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");
        builder.UseSetting("ConnectionStrings:DefaultConnection", _connectionString);
        builder.UseSetting("Cors:AllowedOrigins:0", "http://localhost:5173");

        if (!string.IsNullOrWhiteSpace(_databaseProvider))
        {
            builder.UseSetting("Database:Provider", _databaseProvider);
        }

        builder.ConfigureTestServices(services =>
        {
            if (_dbConnection is not null)
            {
                services.RemoveAll<ApplicationDbContext>();
                services.RemoveAll<DbContextOptions>();
                services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                services.RemoveAll<DbConnection>();
                services.AddSingleton(_dbConnection);
                services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
                    options.UseSqlite(serviceProvider.GetRequiredService<DbConnection>()));
            }

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

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _dbConnection?.Dispose();
        }
    }
}
