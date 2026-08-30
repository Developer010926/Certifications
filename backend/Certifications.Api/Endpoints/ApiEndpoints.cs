namespace Certifications.Api.Endpoints;

internal static class ApiEndpoints
{
    public static void MapApiEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/api/v1");
        api.MapAuthEndpoints();
        api.MapEmployeeEndpoints();
        api.MapContractEndpoints();
        api.MapCertificationEndpoints();
    }
}
