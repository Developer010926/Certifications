using Certifications.Domain.Enums;
using Certifications.Domain.Exceptions;
using Certifications.Domain.Internal;

namespace Certifications.Domain.Entities;

public sealed class Contract
{
    public const int DefaultProlongationWarningMonths = 3;
    public const int DefaultProlongationAlertMonths = 1;
    public const int DefaultProlongationForYears = 1;

    private readonly List<Prolongation> _prolongations = [];

    private Contract()
    {
        Position = null!;
    }

    private Contract(
        long id,
        Guid employeeId,
        string position,
        string? department,
        string? division,
        DateOnly contractDate,
        DateOnly? validTo,
        int prolongationWarningMonths,
        int prolongationAlertMonths,
        int prolongationForYears)
    {
        ValidateRenewalSettings(
            prolongationWarningMonths,
            prolongationAlertMonths,
            prolongationForYears);

        Id = id;
        EmployeeId = employeeId;
        Position = DomainGuard.Required(position, nameof(Position));
        Department = department;
        Division = division;
        ContractDate = contractDate;
        ValidTo = validTo;
        Active = true;
        ProlongationWarningMonths = prolongationWarningMonths;
        ProlongationAlertMonths = prolongationAlertMonths;
        ProlongationForYears = prolongationForYears;
    }

    public long Id { get; private set; }

    public Guid EmployeeId { get; private set; }

    public string Position { get; private set; }

    public string? Department { get; private set; }

    public string? Division { get; private set; }

    public DateOnly ContractDate { get; private set; }

    public DateOnly? ValidTo { get; private set; }

    public bool Active { get; private set; }

    public int ProlongationWarningMonths { get; private set; }

    public int ProlongationAlertMonths { get; private set; }

    public int ProlongationForYears { get; private set; }

    public uint RowVersion { get; private set; }

    public IReadOnlyCollection<Prolongation> Prolongations => _prolongations;

    public DateOnly EffectiveValidTo => ValidTo ?? ContractDate.AddYears(ProlongationForYears);

    public static Contract Create(
        long id,
        Guid employeeId,
        string position,
        DateOnly contractDate,
        string? department = null,
        string? division = null,
        DateOnly? validTo = null,
        int prolongationWarningMonths = DefaultProlongationWarningMonths,
        int prolongationAlertMonths = DefaultProlongationAlertMonths,
        int prolongationForYears = DefaultProlongationForYears)
    {
        if (employeeId == Guid.Empty)
        {
            throw new DomainRuleException(
                DomainErrorCodes.InvalidEmployeeId,
                "EmployeeId must not be empty.");
        }

        return new Contract(
            id,
            employeeId,
            position,
            department,
            division,
            contractDate,
            validTo,
            prolongationWarningMonths,
            prolongationAlertMonths,
            prolongationForYears);
    }

    public void UpdateRenewalSettings(
        int prolongationWarningMonths,
        int prolongationAlertMonths,
        int prolongationForYears)
    {
        ValidateRenewalSettings(
            prolongationWarningMonths,
            prolongationAlertMonths,
            prolongationForYears);

        ProlongationWarningMonths = prolongationWarningMonths;
        ProlongationAlertMonths = prolongationAlertMonths;
        ProlongationForYears = prolongationForYears;
    }

    public void Close(DateOnly closedOn)
    {
        if (!Active)
        {
            throw new DomainRuleException(
                DomainErrorCodes.ContractAlreadyClosed,
                "The contract is already closed.");
        }

        Active = false;
        ValidTo ??= closedOn;
    }

    public void AddProlongation(Prolongation prolongation)
    {
        EnsureActive();
        ArgumentNullException.ThrowIfNull(prolongation);

        if (prolongation.ContractId != Id)
        {
            throw new DomainRuleException(
                DomainErrorCodes.CertificationContractMismatch,
                "The certification belongs to a different contract.");
        }

        if (_prolongations.Any(existing => !existing.IsCompleted))
        {
            throw new DomainRuleException(
                DomainErrorCodes.CertificationInProgressExists,
                "A new certification cannot be added while another certification is in progress.");
        }

        _prolongations.Add(prolongation);
    }

    public CertificationStatus CompleteProlongation(long prolongationId, DateOnly returnedDate)
    {
        EnsureActive();

        var prolongation = _prolongations.SingleOrDefault(item => item.Id == prolongationId);
        if (prolongation is null)
        {
            throw new DomainRuleException(
                DomainErrorCodes.CertificationNotFound,
                "The certification was not found on this contract.");
        }

        prolongation.Complete(returnedDate);
        ValidTo = prolongation.ProtocolDate!.Value.AddYears(ProlongationForYears);

        return CertificationStatus.ContractValid;
    }

    private static void ValidateRenewalSettings(
        int prolongationWarningMonths,
        int prolongationAlertMonths,
        int prolongationForYears)
    {
        if (prolongationWarningMonths < 0
            || prolongationAlertMonths < 0
            || prolongationForYears <= 0
            || prolongationAlertMonths >= prolongationWarningMonths)
        {
            throw new DomainRuleException(
                DomainErrorCodes.InvalidRenewalSettings,
                "Renewal settings must use non-negative month thresholds, a positive year value, and an alert threshold below the warning threshold.");
        }
    }

    private void EnsureActive()
    {
        if (!Active)
        {
            throw new DomainRuleException(
                DomainErrorCodes.ContractInactive,
                "Certifications can only be changed for an active contract.");
        }
    }
}
