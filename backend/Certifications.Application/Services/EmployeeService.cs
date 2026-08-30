using Certifications.Application.Abstractions;
using Certifications.Application.Common;
using Certifications.Application.Contracts;
using Certifications.Domain.Entities;
using Certifications.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace Certifications.Application.Services;

public sealed class EmployeeService(
    IApplicationDbContext dbContext,
    IPasswordGenerator passwordGenerator,
    IPasswordProtector passwordProtector,
    IBusinessClock clock)
{
    public async Task<PagedResult<EmployeeSummaryDto>> ListAsync(
        EmployeeListQuery query,
        CancellationToken cancellationToken)
    {
        RequestValidator.ValidatePagination(query.Page, query.PageSize);
        RequestValidator.ValidateSort(query.Sort, query.Direction, "name", "personalId");

        var employees = dbContext.Employees
            .AsNoTracking()
            .AsQueryable();

        if (!query.IncludeInactive)
        {
            employees = employees.Where(
                employee => employee.Contracts.Any(contract => contract.Active));
        }

        if (!string.IsNullOrWhiteSpace(query.Name))
        {
            var name = query.Name.Trim().ToUpperInvariant();
            employees = employees.Where(employee =>
                employee.PersonalId.ToUpper().Contains(name)
                || employee.FirstName.ToUpper().Contains(name)
                || employee.LastName.ToUpper().Contains(name)
                || (employee.MiddleName != null && employee.MiddleName.ToUpper().Contains(name)));
        }

        var descending = query.Direction.Equals("desc", StringComparison.OrdinalIgnoreCase);
        employees = query.Sort.ToLowerInvariant() switch
        {
            "personalid" => descending
                ? employees.OrderByDescending(employee => employee.PersonalId)
                : employees.OrderBy(employee => employee.PersonalId),
            _ => descending
                ? employees.OrderByDescending(employee => employee.LastName)
                    .ThenByDescending(employee => employee.FirstName)
                : employees.OrderBy(employee => employee.LastName)
                    .ThenBy(employee => employee.FirstName)
        };

        var totalCount = await employees.CountAsync(cancellationToken);
        var page = await employees
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Include(employee => employee.Contracts.Where(contract => contract.Active))
            .ThenInclude(contract => contract.Prolongations)
            .ToListAsync(cancellationToken);

        return new PagedResult<EmployeeSummaryDto>(
            page.Select(employee => DtoMapper.ToSummary(employee, clock.Today)).ToArray(),
            query.Page,
            query.PageSize,
            totalCount);
    }

    public async Task<EmployeeDetailsDto> GetAsync(
        Guid employeeId,
        CancellationToken cancellationToken)
    {
        var employee = await LoadEmployeeAsync(employeeId, cancellationToken);
        return DtoMapper.ToDetails(employee, clock.Today);
    }

    public async Task<CreateEmployeeResultDto> CreateAsync(
        CreateEmployeeRequest request,
        CancellationToken cancellationToken)
    {
        RequestValidator.Validate(request);
        var normalizedPersonalId = PersonalIdNormalizer.Normalize(request.PersonalId);

        if (await dbContext.Employees.AnyAsync(
                employee => employee.NormalizedPersonalId == normalizedPersonalId,
                cancellationToken))
        {
            throw DuplicatePersonalId();
        }

        var employeeId = Guid.NewGuid();
        var firstContract = CreateContract(employeeId, request.FirstContract);
        var password = passwordGenerator.Generate();
        var employee = Employee.Create(
            employeeId,
            request.PersonalId,
            request.FirstName,
            request.MiddleName,
            request.LastName,
            passwordProtector.Protect(password),
            request.IsAdmin,
            firstContract);

        dbContext.Employees.Add(employee);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new CreateEmployeeResultDto(
            DtoMapper.ToDetails(employee, clock.Today),
            password);
    }

    public async Task<EmployeeDetailsDto> UpdateAsync(
        Guid employeeId,
        UpdateEmployeeRequest request,
        CancellationToken cancellationToken)
    {
        RequestValidator.Validate(request);
        var employee = await LoadEmployeeAsync(employeeId, cancellationToken);
        var normalizedPersonalId = PersonalIdNormalizer.Normalize(request.PersonalId);

        if (await dbContext.Employees.AnyAsync(
                item => item.Id != employeeId
                    && item.NormalizedPersonalId == normalizedPersonalId,
                cancellationToken))
        {
            throw DuplicatePersonalId();
        }

        employee.UpdatePersonalId(request.PersonalId);
        employee.UpdateProfile(request.FirstName, request.MiddleName, request.LastName);
        employee.SetAdministrator(request.IsAdmin);
        await dbContext.SaveChangesAsync(cancellationToken);

        return DtoMapper.ToDetails(employee, clock.Today);
    }

    public async Task<PasswordDto> GeneratePasswordAsync(
        Guid employeeId,
        CancellationToken cancellationToken)
    {
        var employee = await dbContext.Employees
            .SingleOrDefaultAsync(item => item.Id == employeeId, cancellationToken)
            ?? throw EmployeeNotFound();
        var password = passwordGenerator.Generate();
        employee.ReplaceEncryptedPassword(passwordProtector.Protect(password));
        await dbContext.SaveChangesAsync(cancellationToken);
        return new PasswordDto(password);
    }

    public async Task<PasswordDto> RevealPasswordAsync(
        Guid employeeId,
        CancellationToken cancellationToken)
    {
        var encryptedPassword = await dbContext.Employees
            .AsNoTracking()
            .Where(employee => employee.Id == employeeId)
            .Select(employee => employee.EncryptedPassword)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw EmployeeNotFound();

        try
        {
            return new PasswordDto(passwordProtector.Unprotect(encryptedPassword));
        }
        catch
        {
            throw new BusinessConflictException(
                "password.not_provisioned",
                "A usable password has not been provisioned for this employee.");
        }
    }

    private async Task<Employee> LoadEmployeeAsync(
        Guid employeeId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Employees
            .Include(employee => employee.Contracts)
            .ThenInclude(contract => contract.Prolongations)
            .SingleOrDefaultAsync(employee => employee.Id == employeeId, cancellationToken)
            ?? throw EmployeeNotFound();
    }

    private static Contract CreateContract(Guid employeeId, CreateContractRequest request)
    {
        return Contract.Create(
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
    }

    private static ResourceNotFoundException EmployeeNotFound() =>
        new("employee.not_found", "The employee was not found.");

    private static BusinessConflictException DuplicatePersonalId() =>
        new("employee.personal_id_conflict", "The normalized personal ID is already in use.");
}
