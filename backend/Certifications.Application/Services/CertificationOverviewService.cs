using Certifications.Application.Abstractions;
using Certifications.Application.Common;
using Certifications.Application.Contracts;
using Certifications.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Certifications.Application.Services;

public sealed class CertificationOverviewService(
    IApplicationDbContext dbContext,
    IBusinessClock clock)
{
    public async Task<PagedResult<CertificationOverviewRowDto>> GetAsync(
        CertificationOverviewQuery query,
        CancellationToken cancellationToken)
    {
        RequestValidator.ValidatePagination(query.Page, query.PageSize);
        RequestValidator.ValidateSort(
            query.Sort,
            query.Direction,
            "name",
            "department",
            "effectiveValidTo",
            "status");

        var today = clock.Today;
        var rows =
            from employee in dbContext.Employees.AsNoTracking()
            join activeContract in dbContext.Contracts.AsNoTracking().Where(item => item.Active)
                on employee.Id equals activeContract.EmployeeId into activeContracts
            from contract in activeContracts.DefaultIfEmpty()
            select new
            {
                Employee = employee,
                Contract = contract
            };

        if (!query.IncludeInactive)
        {
            rows = rows.Where(row => row.Contract != null);
        }

        if (!string.IsNullOrWhiteSpace(query.Name))
        {
            var name = query.Name.Trim().ToUpperInvariant();
            rows = rows.Where(row =>
                row.Employee.PersonalId.ToUpper().Contains(name)
                || row.Employee.FirstName.ToUpper().Contains(name)
                || row.Employee.LastName.ToUpper().Contains(name)
                || (row.Employee.MiddleName != null
                    && row.Employee.MiddleName.ToUpper().Contains(name)));
        }

        if (!string.IsNullOrWhiteSpace(query.Department))
        {
            var department = query.Department.Trim().ToUpperInvariant();
            rows = rows.Where(row =>
                row.Contract != null
                && row.Contract.Department != null
                && row.Contract.Department.ToUpper().Contains(department));
        }

        if (query.Status.HasValue)
        {
            rows = query.Status.Value switch
            {
                CertificationStatus.NotApplicable =>
                    rows.Where(row => row.Contract == null),
                CertificationStatus.CertificationInProgress =>
                    rows.Where(row => row.Contract != null
                        && dbContext.Prolongations.Any(certification =>
                            certification.ContractId == row.Contract.Id
                            && certification.ProlongationReturned == null)),
                CertificationStatus.ContractValid =>
                    rows.Where(row => row.Contract != null
                        && !dbContext.Prolongations.Any(certification =>
                            certification.ContractId == row.Contract.Id
                            && certification.ProlongationReturned == null)
                        && today < (row.Contract.ValidTo
                                ?? row.Contract.ContractDate.AddYears(row.Contract.ProlongationForYears))
                            .AddMonths(-row.Contract.ProlongationWarningMonths)),
                CertificationStatus.CertificationPending =>
                    rows.Where(row => row.Contract != null
                        && !dbContext.Prolongations.Any(certification =>
                            certification.ContractId == row.Contract.Id
                            && certification.ProlongationReturned == null)
                        && today >= (row.Contract.ValidTo
                                ?? row.Contract.ContractDate.AddYears(row.Contract.ProlongationForYears))
                            .AddMonths(-row.Contract.ProlongationWarningMonths)
                        && today < (row.Contract.ValidTo
                                ?? row.Contract.ContractDate.AddYears(row.Contract.ProlongationForYears))
                            .AddMonths(-row.Contract.ProlongationAlertMonths)),
                _ => rows.Where(row => row.Contract != null
                    && !dbContext.Prolongations.Any(certification =>
                        certification.ContractId == row.Contract.Id
                        && certification.ProlongationReturned == null)
                    && today >= (row.Contract.ValidTo
                            ?? row.Contract.ContractDate.AddYears(row.Contract.ProlongationForYears))
                        .AddMonths(-row.Contract.ProlongationAlertMonths))
            };
        }

        if (query.ValidToFrom.HasValue)
        {
            rows = rows.Where(row => row.Contract != null
                && (row.Contract.ValidTo
                    ?? row.Contract.ContractDate.AddYears(row.Contract.ProlongationForYears))
                    >= query.ValidToFrom.Value);
        }

        if (query.ValidToTo.HasValue)
        {
            rows = rows.Where(row => row.Contract != null
                && (row.Contract.ValidTo
                    ?? row.Contract.ContractDate.AddYears(row.Contract.ProlongationForYears))
                    <= query.ValidToTo.Value);
        }

        var totalCount = await rows.CountAsync(cancellationToken);
        var descending = query.Direction.Equals("desc", StringComparison.OrdinalIgnoreCase);
        rows = query.Sort.ToLowerInvariant() switch
        {
            "department" => descending
                ? rows.OrderByDescending(row => row.Contract == null ? null : row.Contract.Department)
                    .ThenByDescending(row => row.Employee.LastName)
                : rows.OrderBy(row => row.Contract == null ? null : row.Contract.Department)
                    .ThenBy(row => row.Employee.LastName),
            "effectivevalidto" => descending
                ? rows.OrderByDescending(row => row.Contract == null
                    ? (DateOnly?)null
                    : row.Contract.ValidTo
                        ?? row.Contract.ContractDate.AddYears(row.Contract.ProlongationForYears))
                : rows.OrderBy(row => row.Contract == null
                    ? (DateOnly?)null
                    : row.Contract.ValidTo
                        ?? row.Contract.ContractDate.AddYears(row.Contract.ProlongationForYears)),
            "status" => descending
                ? rows.OrderByDescending(row => row.Contract == null
                    ? CertificationStatus.NotApplicable
                    : dbContext.Prolongations.Any(certification =>
                        certification.ContractId == row.Contract.Id
                        && certification.ProlongationReturned == null)
                        ? CertificationStatus.CertificationInProgress
                        : today < (row.Contract.ValidTo
                                ?? row.Contract.ContractDate.AddYears(row.Contract.ProlongationForYears))
                            .AddMonths(-row.Contract.ProlongationWarningMonths)
                            ? CertificationStatus.ContractValid
                            : today < (row.Contract.ValidTo
                                    ?? row.Contract.ContractDate.AddYears(row.Contract.ProlongationForYears))
                                .AddMonths(-row.Contract.ProlongationAlertMonths)
                                ? CertificationStatus.CertificationPending
                                : CertificationStatus.CertificationMissing)
                : rows.OrderBy(row => row.Contract == null
                    ? CertificationStatus.NotApplicable
                    : dbContext.Prolongations.Any(certification =>
                        certification.ContractId == row.Contract.Id
                        && certification.ProlongationReturned == null)
                        ? CertificationStatus.CertificationInProgress
                        : today < (row.Contract.ValidTo
                                ?? row.Contract.ContractDate.AddYears(row.Contract.ProlongationForYears))
                            .AddMonths(-row.Contract.ProlongationWarningMonths)
                            ? CertificationStatus.ContractValid
                            : today < (row.Contract.ValidTo
                                    ?? row.Contract.ContractDate.AddYears(row.Contract.ProlongationForYears))
                                .AddMonths(-row.Contract.ProlongationAlertMonths)
                                ? CertificationStatus.CertificationPending
                                : CertificationStatus.CertificationMissing),
            _ => descending
                ? rows.OrderByDescending(row => row.Employee.LastName)
                    .ThenByDescending(row => row.Employee.FirstName)
                : rows.OrderBy(row => row.Employee.LastName)
                    .ThenBy(row => row.Employee.FirstName)
        };

        var page = await rows
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(row => new CertificationOverviewRowDto(
                row.Employee.Id,
                row.Employee.PersonalId,
                row.Employee.FirstName,
                row.Employee.MiddleName,
                row.Employee.LastName,
                row.Employee.IsAdmin,
                row.Contract == null ? null : row.Contract.Id,
                row.Contract == null ? null : row.Contract.Position,
                row.Contract == null ? null : row.Contract.Department,
                row.Contract == null ? null : row.Contract.Division,
                row.Contract == null ? null : row.Contract.ContractDate,
                row.Contract == null
                    ? null
                    : row.Contract.ValidTo
                        ?? row.Contract.ContractDate.AddYears(row.Contract.ProlongationForYears),
                row.Contract == null
                    ? CertificationStatus.NotApplicable
                    : dbContext.Prolongations.Any(certification =>
                        certification.ContractId == row.Contract.Id
                        && certification.ProlongationReturned == null)
                        ? CertificationStatus.CertificationInProgress
                        : today < (row.Contract.ValidTo
                                ?? row.Contract.ContractDate.AddYears(row.Contract.ProlongationForYears))
                            .AddMonths(-row.Contract.ProlongationWarningMonths)
                            ? CertificationStatus.ContractValid
                            : today < (row.Contract.ValidTo
                                    ?? row.Contract.ContractDate.AddYears(row.Contract.ProlongationForYears))
                                .AddMonths(-row.Contract.ProlongationAlertMonths)
                                ? CertificationStatus.CertificationPending
                                : CertificationStatus.CertificationMissing,
                row.Contract == null
                    ? null
                    : dbContext.Prolongations
                        .Where(certification => certification.ContractId == row.Contract.Id)
                        .OrderByDescending(certification => certification.CertificationDate)
                        .ThenByDescending(certification => certification.Id)
                        .Select(certification => new CertificationDto(
                            certification.Id,
                            certification.ContractId,
                            certification.Assessor,
                            certification.CertificationDate,
                            certification.ProtocolDate,
                            certification.ProlongationSend,
                            certification.ProlongationReturned,
                            certification.ProlongationReturned != null))
                        .FirstOrDefault()))
            .ToListAsync(cancellationToken);

        return new PagedResult<CertificationOverviewRowDto>(
            page,
            query.Page,
            query.PageSize,
            totalCount);
    }
}
