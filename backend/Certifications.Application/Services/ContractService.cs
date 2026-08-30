using Certifications.Application.Abstractions;
using Certifications.Application.Common;
using Certifications.Application.Contracts;
using Certifications.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Certifications.Application.Services;

public sealed class ContractService(
    IApplicationDbContext dbContext,
    IBusinessClock clock)
{
    public async Task<ContractDetailsDto> GetCurrentAsync(
        Guid employeeId,
        CancellationToken cancellationToken)
    {
        var contract = await dbContext.Contracts
            .AsNoTracking()
            .Include(item => item.Prolongations)
            .SingleOrDefaultAsync(
                item => item.EmployeeId == employeeId && item.Active,
                cancellationToken)
            ?? throw new ResourceNotFoundException(
                "contract.active_not_found",
                "An active contract was not found.");

        return DtoMapper.ToContractDetails(contract, clock.Today);
    }

    public async Task<ContractDto> CreateAsync(
        Guid employeeId,
        CreateContractRequest request,
        CancellationToken cancellationToken)
    {
        RequestValidator.Validate(request);
        var employee = await dbContext.Employees
            .Include(item => item.Contracts)
            .SingleOrDefaultAsync(item => item.Id == employeeId, cancellationToken)
            ?? throw new ResourceNotFoundException(
                "employee.not_found",
                "The employee was not found.");

        if (employee.ActiveContract is not null)
        {
            throw new BusinessConflictException(
                "contract.active_already_exists",
                "The employee already has an active contract.");
        }

        var contract = Contract.Create(
            0,
            employeeId,
            request.Position,
            request.ContractDate,
            request.Department,
            request.Division,
            request.ValidTo,
            request.ProlongationWarningMonths ?? Contract.DefaultProlongationWarningMonths,
            request.ProlongationAlertMonths ?? Contract.DefaultProlongationAlertMonths,
            request.ProlongationForYears ?? Contract.DefaultProlongationForYears);
        employee.AddContract(contract);
        await dbContext.SaveChangesAsync(cancellationToken);
        return DtoMapper.ToContract(contract, clock.Today);
    }

    public async Task CloseAsync(
        long contractId,
        CloseContractRequest request,
        CancellationToken cancellationToken)
    {
        var contract = await dbContext.Contracts
            .Include(item => item.Prolongations)
            .SingleOrDefaultAsync(item => item.Id == contractId, cancellationToken)
            ?? throw new ResourceNotFoundException(
                "contract.not_found",
                "The contract was not found.");

        dbContext.SetContractOriginalRowVersion(contract, request.RowVersion);
        contract.Close(request.ClosedOn);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new BusinessConflictException(
                "contract.concurrency_conflict",
                "The contract was changed by another request.");
        }
    }
}
