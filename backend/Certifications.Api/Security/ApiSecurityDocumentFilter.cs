using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Certifications.Api.Security;

internal sealed class ApiSecurityDocumentFilter : IDocumentFilter
{
    private const string ApiKeyScheme = "ApiKey";
    private const string CookieScheme = "CookieAuth";

    public void Apply(OpenApiDocument document, DocumentFilterContext context)
    {
        _ = context;

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??=
            new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes[ApiKeyScheme] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.ApiKey,
            In = ParameterLocation.Header,
            Name = "X-API-Key",
            Description = "Additional API-channel key required by every /api/v1 operation."
        };
        document.Components.SecuritySchemes[CookieScheme] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.ApiKey,
            In = ParameterLocation.Cookie,
            Name = "Certifications.Auth",
            Description = "Secure HttpOnly authentication cookie issued by Login."
        };

        var apiKeyReference = new OpenApiSecuritySchemeReference(
            ApiKeyScheme,
            document,
            null);
        var cookieReference = new OpenApiSecuritySchemeReference(
            CookieScheme,
            document,
            null);

        document.Security =
        [
            new OpenApiSecurityRequirement
            {
                [apiKeyReference] = []
            }
        ];

        foreach (var path in document.Paths)
        {
            if (path.Key.Equals("/api/v1/auth/login", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (path.Value?.Operations is null)
            {
                continue;
            }

            foreach (var operation in path.Value.Operations.Values)
            {
                operation.Security =
                [
                    new OpenApiSecurityRequirement
                    {
                        [apiKeyReference] = [],
                        [cookieReference] = []
                    }
                ];
            }
        }
    }
}
