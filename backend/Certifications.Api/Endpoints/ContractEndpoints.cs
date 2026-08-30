using System.Security.Claims;
using Certifications.Api.Security;
using Certifications.Application.Contracts;
using Certifications.Application.Services;

namespace Certifications.Api.Endpoints;

internal static class ContractEndpoints
{
    public static void MapContractEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/employees/{employeeId:guid}/contracts/current", GetCurrentAsync)
            .WithTags("Contracts")
            .WithName("GetCurrentEmployeeContract")
            .WithSummary("Get an employee's active contract")
            .RequireAuthorization("AdminOnly")
            .Produces<ContractDetailsDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        endpoints.MapPost("/employees/{employeeId:guid}/contracts", CreateAsync)
            .WithTags("Contracts")
            .WithName("CreateEmployeeContract")
            .WithSummary("Create a new active contract after the previous contract is closed")
            .RequireCsrf()
            .RequireAuthorization("AdminOnly")
            .Produces<ContractDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        endpoints.MapPost("/contracts/{contractId:long}/close", CloseAsync)
            .WithTags("Contracts")
            .WithName("CloseContract")
            .WithSummary("Close an active contract")
            .RequireCsrf()
            .RequireAuthorization("AdminOnly")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        endpoints.MapGet("/me/contract", GetOwnContractAsync)
            .WithTags("Contracts")
            .WithName("GetOwnContract")
            .WithSummary("Get the current employee's active contract and certification history")
            .RequireAuthorization("ActiveContractRequired")
            .Produces<ContractDetailsDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GetCurrentAsync(
        Guid employeeId,
        ContractService service,
        CancellationToken cancellationToken) =>
        Results.Ok(await service.GetCurrentAsync(employeeId, cancellationToken));

    private static async Task<IResult> CreateAsync(
        Guid employeeId,
        CreateContractRequest request,
        ContractService service,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(employeeId, request, cancellationToken);
        return Results.Created(
            $"/api/v1/employees/{employeeId}/contracts/current",
            result);
    }

    private static async Task<IResult> CloseAsync(
        long contractId,
        CloseContractRequest request,
        ContractService service,
        CancellationToken cancellationToken)
    {
        await service.CloseAsync(contractId, request, cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> GetOwnContractAsync(
        ClaimsPrincipal principal,
        ContractService service,
        CancellationToken cancellationToken) =>
        Results.Ok(await service.GetCurrentAsync(
            principal.GetEmployeeId(),
            cancellationToken));
}
