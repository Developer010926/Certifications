using System.Security.Claims;
using Certifications.Api.Security;
using Certifications.Application.Contracts;
using Certifications.Application.Services;

namespace Certifications.Api.Endpoints;

internal static class EmployeeEndpoints
{
    public static void MapEmployeeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/employees")
            .WithTags("Employees")
            .RequireAuthorization("AdminOnly");

        group.MapGet("/", ListAsync)
            .WithName("ListEmployees")
            .WithSummary("List employees with server-side filtering and pagination")
            .Produces<PagedResult<EmployeeSummaryDto>>()
            .ProducesValidationProblem();

        group.MapGet("/{employeeId:guid}", GetAsync)
            .WithName("GetEmployee")
            .WithSummary("Get employee details and the current contract")
            .Produces<EmployeeDetailsDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", CreateAsync)
            .WithName("CreateEmployee")
            .WithSummary("Atomically create an employee and first contract")
            .RequireCsrf()
            .Produces<CreateEmployeeResultDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPatch("/{employeeId:guid}", UpdateAsync)
            .WithName("UpdateEmployee")
            .WithSummary("Update editable employee data")
            .RequireCsrf()
            .Produces<EmployeeDetailsDto>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{employeeId:guid}/password/generate", GeneratePasswordAsync)
            .WithName("GenerateEmployeePassword")
            .WithSummary("Generate and replace an employee password")
            .RequireCsrf()
            .Produces<PasswordDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{employeeId:guid}/password/reveal", RevealPasswordAsync)
            .WithName("RevealEmployeePassword")
            .WithSummary("Reveal an employee password to an administrator")
            .RequireCsrf()
            .Produces<PasswordDto>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        endpoints.MapPost("/me/password/reveal", RevealOwnPasswordAsync)
            .WithTags("Passwords")
            .WithName("RevealOwnPassword")
            .WithSummary("Reveal the current employee's password")
            .RequireCsrf()
            .RequireAuthorization("ActiveContractRequired")
            .Produces<PasswordDto>()
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> ListAsync(
        EmployeeService service,
        CancellationToken cancellationToken,
        int page = 1,
        int pageSize = 25,
        string? name = null,
        bool includeInactive = false,
        string sort = "name",
        string direction = "asc") =>
        Results.Ok(await service.ListAsync(
            new EmployeeListQuery
            {
                Page = page,
                PageSize = pageSize,
                Name = name,
                IncludeInactive = includeInactive,
                Sort = sort,
                Direction = direction
            },
            cancellationToken));

    private static async Task<IResult> GetAsync(
        Guid employeeId,
        EmployeeService service,
        CancellationToken cancellationToken) =>
        Results.Ok(await service.GetAsync(employeeId, cancellationToken));

    private static async Task<IResult> CreateAsync(
        CreateEmployeeRequest request,
        EmployeeService service,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        EndpointHelpers.SetNoStore(httpContext.Response);
        return Results.Created($"/api/v1/employees/{result.Employee.EmployeeId}", result);
    }

    private static async Task<IResult> UpdateAsync(
        Guid employeeId,
        UpdateEmployeeRequest request,
        EmployeeService service,
        CancellationToken cancellationToken) =>
        Results.Ok(await service.UpdateAsync(employeeId, request, cancellationToken));

    private static async Task<IResult> GeneratePasswordAsync(
        Guid employeeId,
        EmployeeService service,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await service.GeneratePasswordAsync(employeeId, cancellationToken);
        EndpointHelpers.SetNoStore(httpContext.Response);
        return Results.Ok(result);
    }

    private static async Task<IResult> RevealPasswordAsync(
        Guid employeeId,
        EmployeeService service,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await service.RevealPasswordAsync(employeeId, cancellationToken);
        EndpointHelpers.SetNoStore(httpContext.Response);
        return Results.Ok(result);
    }

    private static async Task<IResult> RevealOwnPasswordAsync(
        ClaimsPrincipal principal,
        EmployeeService service,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await service.RevealPasswordAsync(
            principal.GetEmployeeId(),
            cancellationToken);
        EndpointHelpers.SetNoStore(httpContext.Response);
        return Results.Ok(result);
    }
}
