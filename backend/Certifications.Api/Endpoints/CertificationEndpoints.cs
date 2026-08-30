using System.Security.Claims;
using Certifications.Api.Security;
using Certifications.Application.Contracts;
using Certifications.Application.Services;

namespace Certifications.Api.Endpoints;

internal static class CertificationEndpoints
{
    public static void MapCertificationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/contracts/{contractId:long}/certifications", ListAsync)
            .WithTags("Certifications")
            .WithName("ListContractCertifications")
            .WithSummary("List certification history for an owned or administratively accessible contract")
            .RequireAuthorization("ActiveContractRequired")
            .Produces<IReadOnlyList<CertificationDto>>()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        endpoints.MapPost("/contracts/{contractId:long}/certifications", CreateAsync)
            .WithTags("Certifications")
            .WithName("CreateCertification")
            .WithSummary("Create a certification for an active contract")
            .RequireCsrf()
            .RequireAuthorization("AdminOnly")
            .Produces<CertificationDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        endpoints.MapPatch("/certifications/{certificationId:long}", UpdateAsync)
            .WithTags("Certifications")
            .WithName("UpdateCertification")
            .WithSummary("Update an unfinished certification")
            .RequireCsrf()
            .RequireAuthorization("AdminOnly")
            .Produces<CertificationDto>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        endpoints.MapPost("/certifications/{certificationId:long}/return", ReturnAsync)
            .WithTags("Certifications")
            .WithName("ReturnCertification")
            .WithSummary("Atomically complete a certification and extend its contract")
            .RequireCsrf()
            .RequireAuthorization("AdminOnly")
            .Produces<ReturnCertificationResultDto>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        endpoints.MapGet("/certifications/overview", OverviewAsync)
            .WithTags("Certifications")
            .WithName("GetCertificationOverview")
            .WithSummary("Get the server-filtered certification overview")
            .RequireAuthorization("AdminOnly")
            .Produces<PagedResult<CertificationOverviewRowDto>>()
            .ProducesValidationProblem();
    }

    private static async Task<IResult> ListAsync(
        long contractId,
        ClaimsPrincipal principal,
        AuthenticationService authenticationService,
        CertificationService certificationService,
        CancellationToken cancellationToken)
    {
        await authenticationService.EnsureContractReadAccessAsync(
            principal.GetEmployeeId(),
            contractId,
            cancellationToken);
        return Results.Ok(await certificationService.ListAsync(contractId, cancellationToken));
    }

    private static async Task<IResult> CreateAsync(
        long contractId,
        CreateCertificationRequest request,
        CertificationService service,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(contractId, request, cancellationToken);
        return Results.Created(
            $"/api/v1/contracts/{contractId}/certifications",
            result);
    }

    private static async Task<IResult> UpdateAsync(
        long certificationId,
        UpdateCertificationRequest request,
        CertificationService service,
        CancellationToken cancellationToken) =>
        Results.Ok(await service.UpdateAsync(certificationId, request, cancellationToken));

    private static async Task<IResult> ReturnAsync(
        long certificationId,
        ReturnCertificationRequest request,
        CertificationService service,
        CancellationToken cancellationToken) =>
        Results.Ok(await service.ReturnAsync(certificationId, request, cancellationToken));

    private static async Task<IResult> OverviewAsync(
        CertificationOverviewService service,
        CancellationToken cancellationToken,
        int page = 1,
        int pageSize = 25,
        string? name = null,
        string? department = null,
        Certifications.Domain.Enums.CertificationStatus? status = null,
        DateOnly? validToFrom = null,
        DateOnly? validToTo = null,
        bool includeInactive = false,
        string sort = "name",
        string direction = "asc") =>
        Results.Ok(await service.GetAsync(
            new CertificationOverviewQuery
            {
                Page = page,
                PageSize = pageSize,
                Name = name,
                Department = department,
                Status = status,
                ValidToFrom = validToFrom,
                ValidToTo = validToTo,
                IncludeInactive = includeInactive,
                Sort = sort,
                Direction = direction
            },
            cancellationToken));
}
