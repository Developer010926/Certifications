using System.Security.Cryptography;
using System.Text;
using Certifications.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace Certifications.Api.Security;

internal sealed class ApiKeyMiddleware(
    RequestDelegate next,
    IOptions<SecurityOptions> options)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/api/v1"))
        {
            await next(context);
            return;
        }

        var values = context.Request.Headers[options.Value.ApiKeyHeaderName];
        if (values.Count != 1 || !Matches(values[0], options.Value.ApiKey))
        {
            await Results.Problem(
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "API key required",
                    detail: "A valid API key is required.",
                    extensions: new Dictionary<string, object?>
                    {
                        ["code"] = "auth.api_key_invalid",
                        ["traceId"] = context.TraceIdentifier
                    })
                .ExecuteAsync(context);
            return;
        }

        await next(context);
    }

    private static bool Matches(string? provided, string expected)
    {
        if (provided is null)
        {
            return false;
        }

        var providedBytes = Encoding.UTF8.GetBytes(provided);
        var expectedBytes = Encoding.UTF8.GetBytes(expected);

        try
        {
            return providedBytes.Length == expectedBytes.Length
                && CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(providedBytes);
            CryptographicOperations.ZeroMemory(expectedBytes);
        }
    }
}
