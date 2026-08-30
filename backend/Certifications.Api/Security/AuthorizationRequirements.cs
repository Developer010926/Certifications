using Certifications.Application.Services;
using Microsoft.AspNetCore.Authorization;

namespace Certifications.Api.Security;

internal sealed class ActiveContractRequirement : IAuthorizationRequirement;

internal sealed class AdminRequirement : IAuthorizationRequirement;

internal sealed class ActiveContractAuthorizationHandler(
    AuthenticationService authenticationService)
    : AuthorizationHandler<ActiveContractRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ActiveContractRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        Guid employeeId;
        try
        {
            employeeId = context.User.GetEmployeeId();
        }
        catch (UnauthorizedAccessException)
        {
            return;
        }

        var access = await authenticationService.GetAccessAsync(employeeId, CancellationToken.None);
        if (access.Exists && access.HasActiveContract)
        {
            context.Succeed(requirement);
        }
    }
}

internal sealed class AdminAuthorizationHandler(
    AuthenticationService authenticationService)
    : AuthorizationHandler<AdminRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AdminRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        Guid employeeId;
        try
        {
            employeeId = context.User.GetEmployeeId();
        }
        catch (UnauthorizedAccessException)
        {
            return;
        }

        var access = await authenticationService.GetAccessAsync(employeeId, CancellationToken.None);
        if (access.Exists && access.HasActiveContract && access.IsAdmin)
        {
            context.Succeed(requirement);
        }
    }
}
