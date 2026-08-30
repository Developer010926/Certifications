using System.Security.Claims;
using Certifications.Api.Security;
using Certifications.Application.Contracts;
using Certifications.Application.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using ApplicationAuthenticationService = Certifications.Application.Services.AuthenticationService;

namespace Certifications.Api.Endpoints;

internal static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/auth").WithTags("Authentication");

        group.MapPost("/login", LoginAsync)
            .WithName("Login")
            .WithSummary("Authenticate with personal ID and password")
            .Produces<CurrentUserDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/logout", LogoutAsync)
            .WithName("Logout")
            .WithSummary("End the current authenticated session")
            .RequireCsrf()
            .RequireAuthorization("ActiveContractRequired")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapGet("/me", GetMeAsync)
            .WithName("GetCurrentUser")
            .WithSummary("Get the current authenticated user")
            .RequireAuthorization("ActiveContractRequired")
            .Produces<CurrentUserDto>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPut("/preferred-mode", SetPreferredModeAsync)
            .WithName("SetPreferredAdminMode")
            .WithSummary("Set the administrator's preferred application mode")
            .RequireCsrf()
            .RequireAuthorization("AdminOnly")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapGet("/csrf-token", GetCsrfToken)
            .WithName("GetCsrfToken")
            .WithSummary("Issue a CSRF request token for authenticated commands")
            .RequireAuthorization("ActiveContractRequired")
            .Produces<CsrfTokenDto>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        ApplicationAuthenticationService authenticationService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var currentUser = await authenticationService.AuthenticateAsync(request, cancellationToken);
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, currentUser.EmployeeId.ToString())],
            CookieAuthenticationDefaults.AuthenticationScheme);
        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));
        EndpointHelpers.SetNoStore(httpContext.Response);
        return Results.Ok(currentUser);
    }

    private static async Task<IResult> LogoutAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Results.NoContent();
    }

    private static async Task<IResult> GetMeAsync(
        ClaimsPrincipal principal,
        ApplicationAuthenticationService authenticationService,
        CancellationToken cancellationToken)
    {
        return Results.Ok(await authenticationService.GetCurrentUserAsync(
            principal.GetEmployeeId(),
            cancellationToken));
    }

    private static async Task<IResult> SetPreferredModeAsync(
        PreferredModeRequest request,
        ClaimsPrincipal principal,
        ApplicationAuthenticationService authenticationService,
        CancellationToken cancellationToken)
    {
        await authenticationService.SetPreferredModeAsync(
            principal.GetEmployeeId(),
            request,
            cancellationToken);
        return Results.NoContent();
    }

    private static IResult GetCsrfToken(
        IAntiforgery antiforgery,
        HttpContext httpContext)
    {
        var tokens = antiforgery.GetAndStoreTokens(httpContext);
        EndpointHelpers.SetNoStore(httpContext.Response);
        return Results.Ok(new CsrfTokenDto(tokens.RequestToken!));
    }
}
