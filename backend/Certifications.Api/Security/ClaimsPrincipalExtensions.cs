using System.Security.Claims;

namespace Certifications.Api.Security;

internal static class ClaimsPrincipalExtensions
{
    public static Guid GetEmployeeId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var employeeId)
            ? employeeId
            : throw new UnauthorizedAccessException("The authentication ticket is invalid.");
    }
}
