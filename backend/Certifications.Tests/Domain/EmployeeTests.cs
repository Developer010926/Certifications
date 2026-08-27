using Certifications.Domain.Entities;
using Certifications.Domain.Enums;
using Certifications.Domain.Exceptions;

namespace Certifications.Tests.Domain;

public sealed class EmployeeTests
{
    [Fact]
    public void Create_InitializesEmployeeWithFirstContract()
    {
        var employeeId = Guid.NewGuid();
        var firstContract = CreateContract(employeeId, 1);

        var employee = Employee.Create(
            employeeId,
            " emp 001 ",
            "Ada",
            null,
            "Lovelace",
            "encrypted",
            true,
            firstContract,
            AdminMode.Administration);

        Assert.Equal(employeeId, employee.Id);
        Assert.Equal(" emp 001 ", employee.PersonalId);
        Assert.Equal("EMP001", employee.NormalizedPersonalId);
        Assert.Equal(AdminMode.Administration, employee.PreferredAdminMode);
        Assert.Same(firstContract, Assert.Single(employee.Contracts));
        Assert.Same(firstContract, employee.ActiveContract);
    }

    [Fact]
    public void Create_RejectsAnEmptyEmployeeId()
    {
        var firstContract = CreateContract(Guid.NewGuid(), 1);

        var exception = Assert.Throws<DomainRuleException>(() => Employee.Create(
            Guid.Empty,
            "EMP001",
            "Ada",
            null,
            "Lovelace",
            "encrypted",
            false,
            firstContract));

        Assert.Equal(DomainErrorCodes.InvalidEmployeeId, exception.Code);
    }

    [Fact]
    public void Create_RequiresAFirstContract()
    {
        var exception = Assert.Throws<DomainRuleException>(() => Employee.Create(
            Guid.NewGuid(),
            "EMP001",
            "Ada",
            null,
            "Lovelace",
            "encrypted",
            false,
            null!));

        Assert.Equal(DomainErrorCodes.FirstContractRequired, exception.Code);
    }

    [Fact]
    public void Create_RejectsAContractForAnotherEmployee()
    {
        var exception = Assert.Throws<DomainRuleException>(() => Employee.Create(
            Guid.NewGuid(),
            "EMP001",
            "Ada",
            null,
            "Lovelace",
            "encrypted",
            false,
            CreateContract(Guid.NewGuid(), 1)));

        Assert.Equal(DomainErrorCodes.ContractEmployeeMismatch, exception.Code);
    }

    [Theory]
    [InlineData("", "Ada", "Lovelace", "encrypted")]
    [InlineData("EMP001", "", "Lovelace", "encrypted")]
    [InlineData("EMP001", "Ada", "", "encrypted")]
    [InlineData("EMP001", "Ada", "Lovelace", "")]
    public void Create_RejectsMissingRequiredEmployeeValues(
        string personalId,
        string firstName,
        string lastName,
        string encryptedPassword)
    {
        var employeeId = Guid.NewGuid();

        var exception = Assert.Throws<DomainRuleException>(() => Employee.Create(
            employeeId,
            personalId,
            firstName,
            null,
            lastName,
            encryptedPassword,
            false,
            CreateContract(employeeId, 1)));

        Assert.Equal(DomainErrorCodes.RequiredValue, exception.Code);
    }

    [Fact]
    public void AddContract_RejectsASecondActiveContract()
    {
        var employee = CreateEmployee();

        var exception = Assert.Throws<DomainRuleException>(
            () => employee.AddContract(CreateContract(employee.Id, 2)));

        Assert.Equal(DomainErrorCodes.ActiveContractAlreadyExists, exception.Code);
    }

    [Fact]
    public void AddContract_AllowsANewContractAfterTheCurrentContractCloses()
    {
        var employee = CreateEmployee();
        employee.ActiveContract!.Close(new DateOnly(2026, 6, 30));
        var nextContract = CreateContract(employee.Id, 2);

        employee.AddContract(nextContract);

        Assert.Equal(2, employee.Contracts.Count);
        Assert.Same(nextContract, employee.ActiveContract);
    }

    [Fact]
    public void SetPreferredAdminMode_RejectsNonAdministrators()
    {
        var employee = CreateEmployee();

        var exception = Assert.Throws<DomainRuleException>(
            () => employee.SetPreferredAdminMode(AdminMode.Administration));

        Assert.Equal(DomainErrorCodes.AdminModeRequiresAdmin, exception.Code);
    }

    [Fact]
    public void RemovingAdministratorRights_ClearsThePreferredMode()
    {
        var employee = CreateEmployee(isAdmin: true, AdminMode.MyPage);

        employee.SetAdministrator(false);

        Assert.False(employee.IsAdmin);
        Assert.Null(employee.PreferredAdminMode);
    }

    [Fact]
    public void UpdateOperations_ReplaceMutableEmployeeData()
    {
        var employee = CreateEmployee();

        employee.UpdatePersonalId(" new 001 ");
        employee.UpdateProfile("Grace", "Brewster", "Hopper");
        employee.ReplaceEncryptedPassword("new-encrypted-value");

        Assert.Equal(" new 001 ", employee.PersonalId);
        Assert.Equal("NEW001", employee.NormalizedPersonalId);
        Assert.Equal("Grace", employee.FirstName);
        Assert.Equal("Brewster", employee.MiddleName);
        Assert.Equal("Hopper", employee.LastName);
        Assert.Equal("new-encrypted-value", employee.EncryptedPassword);
    }

    private static Employee CreateEmployee(
        bool isAdmin = false,
        AdminMode? preferredAdminMode = null)
    {
        var employeeId = Guid.NewGuid();
        return Employee.Create(
            employeeId,
            "EMP001",
            "Ada",
            null,
            "Lovelace",
            "encrypted",
            isAdmin,
            CreateContract(employeeId, 1),
            preferredAdminMode);
    }

    private static Contract CreateContract(Guid employeeId, long contractId)
    {
        return Contract.Create(
            contractId,
            employeeId,
            "Engineer",
            new DateOnly(2026, 1, 1));
    }
}
