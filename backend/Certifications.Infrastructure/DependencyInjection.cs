using Certifications.Application.Abstractions;
using Certifications.Infrastructure.Bootstrap;
using Certifications.Infrastructure.Configuration;
using Certifications.Infrastructure.Persistence;
using Certifications.Infrastructure.Security;
using Certifications.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Certifications.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString,
        IConfiguration configuration,
        bool addBootstrapHostedService = true)
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

        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<CertificationsDbContext>());
        services.AddScoped<IPasswordProtector, AesGcmPasswordProtector>();
        services.AddSingleton<IPasswordGenerator, CryptographicPasswordGenerator>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IBusinessClock, BusinessClock>();

        services.AddOptions<SecurityOptions>()
            .Bind(configuration.GetSection(SecurityOptions.SectionName))
            .Validate(ValidateSecurityOptions, "Security options are invalid.")
            .ValidateOnStart();
        services.AddOptions<BusinessOptions>()
            .Bind(configuration.GetSection(BusinessOptions.SectionName))
            .Validate(
                options => TryFindTimeZone(options.TimeZoneId),
                "Business:TimeZoneId is invalid.")
            .ValidateOnStart();
        services.AddOptions<BootstrapAdminOptions>()
            .Bind(configuration.GetSection(BootstrapAdminOptions.SectionName));

        if (addBootstrapHostedService)
        {
            services.AddHostedService<BootstrapAdminHostedService>();
        }

        return services;
    }

    private static bool ValidateSecurityOptions(SecurityOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKeyHeaderName)
            || options.ApiKey.Length < 32)
        {
            return false;
        }

        try
        {
            return Convert.FromBase64String(options.PasswordEncryptionKey).Length == 32;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static bool TryFindTimeZone(string timeZoneId)
    {
        try
        {
            _ = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            return false;
        }
    }
}
