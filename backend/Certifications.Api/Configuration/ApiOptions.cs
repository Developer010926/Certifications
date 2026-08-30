namespace Certifications.Api.Configuration;

public sealed class CookieSettings
{
    public const string SectionName = "Authentication";

    public string CookieName { get; init; } = "Certifications.Auth";

    public int ExpireMinutes { get; init; } = 480;
}

public sealed class CorsSettings
{
    public const string SectionName = "Cors";

    public string[] AllowedOrigins { get; init; } = [];
}
