namespace Certifications.Infrastructure.Configuration;

public sealed class SecurityOptions
{
    public const string SectionName = "Security";

    public string ApiKeyHeaderName { get; init; } = "X-API-Key";

    public string ApiKey { get; init; } = string.Empty;

    public string PasswordEncryptionKey { get; init; } = string.Empty;
}

public sealed class BusinessOptions
{
    public const string SectionName = "Business";

    public string TimeZoneId { get; init; } = "Europe/Vienna";
}

public sealed class BootstrapAdminOptions
{
    public const string SectionName = "BootstrapAdmin";

    public string PersonalId { get; init; } = "КП-0001";

    public string Password { get; init; } = string.Empty;
}
