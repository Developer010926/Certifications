using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Certifications.Infrastructure.Persistence;

public sealed class DesignTimeCertificationsDbContextFactory
    : IDesignTimeDbContextFactory<CertificationsDbContext>
{
    private const string ConnectionStringEnvironmentVariable =
        "ConnectionStrings__DefaultConnection";

    public CertificationsDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CertificationsDbContext>();
        var connectionString = Environment.GetEnvironmentVariable(
            ConnectionStringEnvironmentVariable);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            optionsBuilder.UseNpgsql(ConfigurePostgres);
        }
        else
        {
            optionsBuilder.UseNpgsql(connectionString, ConfigurePostgres);
        }

        return new CertificationsDbContext(optionsBuilder.Options);
    }

    private static void ConfigurePostgres(
        Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure.NpgsqlDbContextOptionsBuilder options)
    {
        options.MigrationsAssembly(typeof(CertificationsDbContext).Assembly.GetName().Name);
    }
}
