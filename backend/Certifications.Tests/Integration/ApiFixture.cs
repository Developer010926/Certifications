using Certifications.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;

namespace Certifications.Tests.Integration;

public sealed class ApiFixture : IAsyncLifetime
{
    public const string ApiKey = "integration-api-key-32-characters";
    public const string BootstrapPassword = "Bootstrap123";

    private readonly PostgreSqlContainer database = new PostgreSqlBuilder("postgres:18-bookworm")
        .WithDatabase("certifications_tests")
        .WithUsername("certifications")
        .WithPassword("certifications-tests-password")
        .Build();

    private CertificationsApiFactory? factory;

    public async Task InitializeAsync()
    {
        await database.StartAsync();

        var options = new DbContextOptionsBuilder<CertificationsDbContext>()
            .UseNpgsql(database.GetConnectionString())
            .Options;
        await using (var dbContext = new CertificationsDbContext(options))
        {
            await dbContext.Database.MigrateAsync();
        }

        Environment.SetEnvironmentVariable(
            "ConnectionStrings__DefaultConnection",
            database.GetConnectionString());
        factory = new CertificationsApiFactory(database.GetConnectionString());

        // Force startup so one-time seed administrator provisioning completes first.
        using var client = CreateClient();
        using var response = await client.GetAsync("/api/v1/auth/me");
    }

    public async Task DisposeAsync()
    {
        if (factory is not null)
        {
            await factory.DisposeAsync();
        }

        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", null);
        await database.DisposeAsync();
    }

    public HttpClient CreateClient(bool includeApiKey = true)
    {
        var client = (factory ?? throw new InvalidOperationException("Fixture is not initialized."))
            .CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                BaseAddress = new Uri("https://localhost")
            });

        if (includeApiKey)
        {
            client.DefaultRequestHeaders.Add("X-API-Key", ApiKey);
        }

        return client;
    }

    private sealed class CertificationsApiFactory(string connectionString)
        : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = connectionString,
                    ["Security:ApiKey"] = ApiKey,
                    ["Security:PasswordEncryptionKey"] = Convert.ToBase64String(
                        Enumerable.Range(1, 32).Select(value => (byte)value).ToArray()),
                    ["Business:TimeZoneId"] = "UTC",
                    ["BootstrapAdmin:PersonalId"] = "КП-0001",
                    ["BootstrapAdmin:Password"] = BootstrapPassword,
                    ["Authentication:CookieName"] = "Certifications.Tests.Auth"
                });
            });
        }
    }
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFixture>
{
    public const string Name = "PostgreSQL API";
}
