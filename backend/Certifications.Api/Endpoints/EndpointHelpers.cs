using Certifications.Api.Security;

namespace Certifications.Api.Endpoints;

internal static class EndpointHelpers
{
    public static RouteHandlerBuilder RequireCsrf(this RouteHandlerBuilder builder) =>
        builder.AddEndpointFilter<AntiforgeryEndpointFilter>();

    public static void SetNoStore(HttpResponse response)
    {
        response.Headers.CacheControl = "no-store";
        response.Headers.Pragma = "no-cache";
    }
}
