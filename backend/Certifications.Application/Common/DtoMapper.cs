using Certifications.Application.Contracts;
using Certifications.Domain.Entities;
using Certifications.Domain.Services;

namespace Certifications.Application.Common;

internal static class DtoMapper
{
    public static CurrentUserDto ToCurrentUser(Employee employee)
    {
        return new CurrentUserDto(
            employee.Id,
            employee.PersonalId,
            BuildDisplayName(employee),
            employee.IsAdmin,
            employee.PreferredAdminMode);
    }

    public static EmployeeSummaryDto ToSummary(Employee employee, DateOnly today)
    {
        var activeContract = employee.ActiveContract;

        return new EmployeeSummaryDto(
            employee.Id,
            employee.PersonalId,
            employee.FirstName,
            employee.MiddleName,
            employee.LastName,
            employee.IsAdmin,
            activeContract is not null,
            CertificationStatusCalculator.Calculate(activeContract, today));
    }

    public static EmployeeDetailsDto ToDetails(Employee employee, DateOnly today)
    {
        var activeContract = employee.ActiveContract;

        return new EmployeeDetailsDto(
            employee.Id,
            employee.PersonalId,
            employee.FirstName,
            employee.MiddleName,
            employee.LastName,
            employee.IsAdmin,
            employee.PreferredAdminMode,
            activeContract is null ? null : ToContractDetails(activeContract, today));
    }

    public static ContractDetailsDto ToContractDetails(Contract contract, DateOnly today)
    {
        return new ContractDetailsDto(
            ToContract(contract, today),
            contract.Prolongations
                .OrderByDescending(item => item.CertificationDate)
                .ThenByDescending(item => item.Id)
                .Select(ToCertification)
                .ToArray());
    }

    public static ContractDto ToContract(Contract contract, DateOnly today)
    {
        return new ContractDto(
            contract.Id,
            contract.EmployeeId,
            contract.Position,
            contract.Department,
            contract.Division,
            contract.ContractDate,
            contract.ValidTo,
            contract.EffectiveValidTo,
            contract.Active,
            contract.ProlongationWarningMonths,
            contract.ProlongationAlertMonths,
            contract.ProlongationForYears,
            contract.RowVersion,
            CertificationStatusCalculator.Calculate(contract, today));
    }

    public static CertificationDto ToCertification(Prolongation certification)
    {
        return new CertificationDto(
            certification.Id,
            certification.ContractId,
            certification.Assessor,
            certification.CertificationDate,
            certification.ProtocolDate,
            certification.ProlongationSend,
            certification.ProlongationReturned,
            certification.IsCompleted);
    }

    private static string BuildDisplayName(Employee employee)
    {
        return string.Join(
            ' ',
            new[] { employee.FirstName, employee.MiddleName, employee.LastName }
                .Where(value => !string.IsNullOrWhiteSpace(value)));
    }
}
