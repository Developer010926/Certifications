using Microsoft.AspNetCore.Antiforgery;

namespace Certifications.Api.Security;

internal sealed class AntiforgeryEndpointFilter(IAntiforgery antiforgery)
    : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        try
        {
            await antiforgery.ValidateRequestAsync(context.HttpContext);
        }
        catch (AntiforgeryValidationException)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid CSRF token",
                detail: "A valid CSRF token is required.",
                extensions: new Dictionary<string, object?>
                {
                    ["code"] = "auth.csrf_invalid",
                    ["traceId"] = context.HttpContext.TraceIdentifier
                });
        }

        return await next(context);
    }
}
