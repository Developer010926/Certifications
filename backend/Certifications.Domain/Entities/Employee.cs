using Certifications.Domain.Enums;
using Certifications.Domain.Exceptions;
using Certifications.Domain.Internal;
using Certifications.Domain.Services;

namespace Certifications.Domain.Entities;

public sealed class Employee
{
    private readonly List<Contract> _contracts = [];

    private Employee()
    {
        PersonalId = null!;
        NormalizedPersonalId = null!;
        FirstName = null!;
        LastName = null!;
        EncryptedPassword = null!;
    }

    private Employee(
        Guid id,
        string personalId,
        string firstName,
        string? middleName,
        string lastName,
        string encryptedPassword,
        bool isAdmin,
        AdminMode? preferredAdminMode)
    {
        PersonalId = null!;
        NormalizedPersonalId = null!;
        FirstName = null!;
        LastName = null!;
        Id = id;
        SetPersonalId(personalId);
        SetProfile(firstName, middleName, lastName);
        EncryptedPassword = DomainGuard.Required(encryptedPassword, nameof(EncryptedPassword));
        IsAdmin = isAdmin;

        if (preferredAdminMode.HasValue)
        {
            SetPreferredAdminMode(preferredAdminMode.Value);
        }
    }

    public Guid Id { get; private set; }

    public string PersonalId { get; private set; }

    public string NormalizedPersonalId { get; private set; }

    public string FirstName { get; private set; }

    public string? MiddleName { get; private set; }

    public string LastName { get; private set; }

    public string EncryptedPassword { get; private set; }

    public bool IsAdmin { get; private set; }

    public AdminMode? PreferredAdminMode { get; private set; }

    public IReadOnlyCollection<Contract> Contracts => _contracts;

    public Contract? ActiveContract => _contracts.SingleOrDefault(contract => contract.Active);

    public static Employee Create(
        Guid id,
        string personalId,
        string firstName,
        string? middleName,
        string lastName,
        string encryptedPassword,
        bool isAdmin,
        Contract firstContract,
        AdminMode? preferredAdminMode = null)
    {
        if (id == Guid.Empty)
        {
            throw new DomainRuleException(
                DomainErrorCodes.InvalidEmployeeId,
                "Employee Id must not be empty.");
        }

        if (firstContract is null)
        {
            throw new DomainRuleException(
                DomainErrorCodes.FirstContractRequired,
                "An employee must be created with a first contract.");
        }

        var employee = new Employee(
            id,
            personalId,
            firstName,
            middleName,
            lastName,
            encryptedPassword,
            isAdmin,
            preferredAdminMode);

        employee.AddContract(firstContract);
        return employee;
    }

    public void UpdatePersonalId(string personalId)
    {
        SetPersonalId(personalId);
    }

    public void UpdateProfile(string firstName, string? middleName, string lastName)
    {
        SetProfile(firstName, middleName, lastName);
    }

    public void ReplaceEncryptedPassword(string encryptedPassword)
    {
        EncryptedPassword = DomainGuard.Required(encryptedPassword, nameof(EncryptedPassword));
    }

    public void SetAdministrator(bool isAdmin)
    {
        IsAdmin = isAdmin;

        if (!isAdmin)
        {
            PreferredAdminMode = null;
        }
    }

    public void SetPreferredAdminMode(AdminMode preferredAdminMode)
    {
        if (!IsAdmin)
        {
            throw new DomainRuleException(
                DomainErrorCodes.AdminModeRequiresAdmin,
                "Only an administrator can select an administrator mode.");
        }

        PreferredAdminMode = preferredAdminMode;
    }

    public void AddContract(Contract contract)
    {
        ArgumentNullException.ThrowIfNull(contract);

        if (contract.EmployeeId != Id)
        {
            throw new DomainRuleException(
                DomainErrorCodes.ContractEmployeeMismatch,
                "The contract belongs to a different employee.");
        }

        if (!contract.Active)
        {
            throw new DomainRuleException(
                DomainErrorCodes.ContractMustBeActive,
                "A newly added contract must be active.");
        }

        if (_contracts.Any(existing => existing.Active))
        {
            throw new DomainRuleException(
                DomainErrorCodes.ActiveContractAlreadyExists,
                "The employee already has an active contract.");
        }

        _contracts.Add(contract);
    }

    private void SetPersonalId(string personalId)
    {
        PersonalId = DomainGuard.Required(personalId, nameof(PersonalId));
        NormalizedPersonalId = PersonalIdNormalizer.Normalize(personalId);
    }

    private void SetProfile(string firstName, string? middleName, string lastName)
    {
        FirstName = DomainGuard.Required(firstName, nameof(FirstName));
        MiddleName = middleName;
        LastName = DomainGuard.Required(lastName, nameof(LastName));
    }
}
