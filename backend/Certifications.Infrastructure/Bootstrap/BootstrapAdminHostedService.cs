using Certifications.Application.Abstractions;
using Certifications.Domain.Services;
using Certifications.Infrastructure.Configuration;
using Certifications.Infrastructure.Persistence;
using Certifications.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Certifications.Infrastructure.Bootstrap;

internal sealed class BootstrapAdminHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<BootstrapAdminOptions> options)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CertificationsDbContext>();
        var passwordProtector = scope.ServiceProvider.GetRequiredService<IPasswordProtector>();
        var normalizedPersonalId = PersonalIdNormalizer.Normalize(options.Value.PersonalId);
        var employee = await dbContext.Employees.SingleOrDefaultAsync(
            item => item.NormalizedPersonalId == normalizedPersonalId,
            cancellationToken);

        if (employee is null)
        {
            throw new InvalidOperationException(
                "The configured bootstrap administrator was not found. Apply migrations before starting the API.");
        }

        if (employee.EncryptedPassword != CriminalPoliceSeedData.UnprovisionedPassword)
        {
            return;
        }

        ValidatePassword(options.Value.Password);
        employee.ReplaceEncryptedPassword(passwordProtector.Protect(options.Value.Password));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static void ValidatePassword(string password)
    {
        if (password.Length < 8
            || !password.Any(char.IsLetter)
            || !password.Any(char.IsDigit))
        {
            throw new InvalidOperationException(
                "BootstrapAdmin:Password must contain at least eight characters, a letter, and a digit while the seed administrator is unprovisioned.");
        }
    }
}
