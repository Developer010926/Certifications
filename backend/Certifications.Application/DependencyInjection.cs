using Certifications.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Certifications.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<AuthenticationService>();
        services.AddScoped<EmployeeService>();
        services.AddScoped<ContractService>();
        services.AddScoped<CertificationService>();
        services.AddScoped<CertificationOverviewService>();
        return services;
    }
}
