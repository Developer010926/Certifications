using Certifications.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Certifications.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<CertificationsDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                postgresOptions =>
                {
                    postgresOptions.MigrationsAssembly(
                        typeof(CertificationsDbContext).Assembly.GetName().Name);
                    postgresOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorCodesToAdd: null);
                }));

        return services;
    }
}
