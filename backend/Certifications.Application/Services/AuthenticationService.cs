using Certifications.Application.Abstractions;
using Certifications.Application.Common;
using Certifications.Application.Contracts;
using Certifications.Domain.Entities;
using Certifications.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace Certifications.Application.Services;

public sealed class AuthenticationService(
    IApplicationDbContext dbContext,
    IPasswordProtector passwordProtector)
{
    public async Task<CurrentUserDto> AuthenticateAsync(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        RequestValidator.Validate(request);

        string normalizedPersonalId;
        try
        {
            normalizedPersonalId = PersonalIdNormalizer.Normalize(request.PersonalId);
        }
        catch
        {
            throw new InvalidCredentialsException();
        }

        var employee = await dbContext.Employees
            .AsNoTracking()
            .Include(item => item.Contracts)
            .SingleOrDefaultAsync(
                item => item.NormalizedPersonalId == normalizedPersonalId,
                cancellationToken);

        if (employee?.ActiveContract is null
            || !passwordProtector.Verify(request.Password, employee.EncryptedPassword))
        {
            throw new InvalidCredentialsException();
        }

        return DtoMapper.ToCurrentUser(employee);
    }

    public async Task<CurrentUserDto> GetCurrentUserAsync(
        Guid employeeId,
        CancellationToken cancellationToken)
    {
        var employee = await FindActiveEmployeeAsync(employeeId, cancellationToken);
        return DtoMapper.ToCurrentUser(employee);
    }

    public async Task SetPreferredModeAsync(
        Guid employeeId,
        PreferredModeRequest request,
        CancellationToken cancellationToken)
    {
        var employee = await FindActiveEmployeeAsync(employeeId, cancellationToken);

        if (!employee.IsAdmin)
        {
            throw new AccessDeniedException(
                "auth.admin_required",
                "Administrator access is required.");
        }

        employee.SetPreferredAdminMode(request.PreferredMode);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<UserAccessDto> GetAccessAsync(
        Guid employeeId,
        CancellationToken cancellationToken)
    {
        var access = await dbContext.Employees
            .AsNoTracking()
            .Where(employee => employee.Id == employeeId)
            .Select(employee => new UserAccessDto(
                employee.Id,
                true,
                employee.Contracts.Any(contract => contract.Active),
                employee.IsAdmin))
            .SingleOrDefaultAsync(cancellationToken);

        return access ?? new UserAccessDto(employeeId, false, false, false);
    }

    public async Task EnsureContractReadAccessAsync(
        Guid employeeId,
        long contractId,
        CancellationToken cancellationToken)
    {
        var access = await GetAccessAsync(employeeId, cancellationToken);
        if (!access.Exists || !access.HasActiveContract)
        {
            throw new AccessDeniedException(
                "auth.active_contract_required",
                "An active contract is required.");
        }

        if (access.IsAdmin)
        {
            return;
        }

        var ownsContract = await dbContext.Contracts.AsNoTracking().AnyAsync(
            contract => contract.Id == contractId && contract.EmployeeId == employeeId,
            cancellationToken);

        if (!ownsContract)
        {
            throw new AccessDeniedException(
                "auth.own_employee_data_required",
                "The requested contract does not belong to the current employee.");
        }
    }

    private async Task<Employee> FindActiveEmployeeAsync(
        Guid employeeId,
        CancellationToken cancellationToken)
    {
        var employee = await dbContext.Employees
            .Include(item => item.Contracts)
            .SingleOrDefaultAsync(item => item.Id == employeeId, cancellationToken);

        if (employee is null)
        {
            throw new ResourceNotFoundException(
                "employee.not_found",
                "The employee was not found.");
        }

        if (employee.ActiveContract is null)
        {
            throw new AccessDeniedException(
                "auth.active_contract_required",
                "An active contract is required.");
        }

        return employee;
    }
}
