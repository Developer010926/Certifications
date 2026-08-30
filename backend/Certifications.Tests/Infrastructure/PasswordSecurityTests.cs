using System.Security.Cryptography;
using Certifications.Application.Abstractions;
using Certifications.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Certifications.Tests.Infrastructure;

public sealed class PasswordSecurityTests
{
    [Fact]
    public void Protector_AuthenticatesCiphertextAndRejectsTampering()
    {
        using var provider = CreateProvider();
        var protector = provider.GetRequiredService<IPasswordProtector>();
        var encrypted = protector.Protect("SecurePassword123");

        Assert.NotEqual("SecurePassword123", encrypted);
        Assert.Equal("SecurePassword123", protector.Unprotect(encrypted));
        Assert.True(protector.Verify("SecurePassword123", encrypted));
        Assert.False(protector.Verify("WrongPassword123", encrypted));

        var parts = encrypted.Split('.');
        parts[2] = (parts[2][0] == 'A' ? 'B' : 'A') + parts[2][1..];
        var tampered = string.Join('.', parts);
        Assert.ThrowsAny<CryptographicException>(() => protector.Unprotect(tampered));
    }

    [Fact]
    public void Generator_AlwaysIncludesLettersAndDigits()
    {
        using var provider = CreateProvider();
        var generator = provider.GetRequiredService<IPasswordGenerator>();

        var passwords = Enumerable.Range(0, 100)
            .Select(_ => generator.Generate())
            .ToArray();

        Assert.All(passwords, password =>
        {
            Assert.Equal(16, password.Length);
            Assert.Contains(password, char.IsLetter);
            Assert.Contains(password, char.IsDigit);
        });
        Assert.Equal(passwords.Length, passwords.Distinct().Count());
    }

    private static ServiceProvider CreateProvider()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Security:ApiKey"] = new string('a', 32),
                ["Security:PasswordEncryptionKey"] = Convert.ToBase64String(
                    Enumerable.Range(1, 32).Select(value => (byte)value).ToArray()),
                ["Business:TimeZoneId"] = "UTC"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddInfrastructure(
            "Host=localhost;Database=unused;Username=unused;Password=unused",
            configuration,
            addBootstrapHostedService: false);
        return services.BuildServiceProvider();
    }
}
