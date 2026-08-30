using Certifications.Domain.Entities;
using Certifications.Domain.Enums;
using Certifications.Domain.Exceptions;

namespace Certifications.Tests.Domain;

public sealed class ContractTests
{
    [Fact]
    public void Create_UsesDefaultRenewalSettings()
    {
        var contract = CreateContract();

        Assert.Equal(3, contract.ProlongationWarningMonths);
        Assert.Equal(1, contract.ProlongationAlertMonths);
        Assert.Equal(1, contract.ProlongationForYears);
        Assert.True(contract.Active);
    }

    [Fact]
    public void Create_RequiresAPosition()
    {
        var exception = Assert.Throws<DomainRuleException>(() => Contract.Create(
            10,
            Guid.NewGuid(),
            " ",
            new DateOnly(2026, 1, 1)));

        Assert.Equal(DomainErrorCodes.RequiredValue, exception.Code);
    }

    [Fact]
    public void EffectiveValidTo_UsesCalculatedDateWhenValidToIsMissing()
    {
        var contract = CreateContract(contractDate: new DateOnly(2024, 2, 29));

        Assert.Equal(new DateOnly(2025, 2, 28), contract.EffectiveValidTo);
        Assert.Null(contract.ValidTo);
    }

    [Fact]
    public void EffectiveValidTo_PrefersExplicitValidTo()
    {
        var validTo = new DateOnly(2028, 5, 20);
        var contract = CreateContract(validTo: validTo);

        Assert.Equal(validTo, contract.EffectiveValidTo);
    }

    [Theory]
    [InlineData(-1, 0, 1)]
    [InlineData(3, -1, 1)]
    [InlineData(3, 1, 0)]
    [InlineData(3, 3, 1)]
    [InlineData(1, 2, 1)]
    public void Create_RejectsInvalidRenewalSettings(
        int warningMonths,
        int alertMonths,
        int years)
    {
        var exception = Assert.Throws<DomainRuleException>(() => CreateContract(
            warningMonths: warningMonths,
            alertMonths: alertMonths,
            years: years));

        Assert.Equal(DomainErrorCodes.InvalidRenewalSettings, exception.Code);
    }

    [Fact]
    public void UpdateRenewalSettings_RevalidatesAndUpdatesValues()
    {
        var contract = CreateContract();

        contract.UpdateRenewalSettings(6, 2, 3);

        Assert.Equal(6, contract.ProlongationWarningMonths);
        Assert.Equal(2, contract.ProlongationAlertMonths);
        Assert.Equal(3, contract.ProlongationForYears);
    }

    [Fact]
    public void Close_UsesClosingDateWhenValidToIsMissing()
    {
        var contract = CreateContract();
        var closedOn = new DateOnly(2026, 8, 27);

        contract.Close(closedOn);

        Assert.False(contract.Active);
        Assert.Equal(closedOn, contract.ValidTo);
    }

    [Fact]
    public void Close_PreservesAnExistingValidTo()
    {
        var validTo = new DateOnly(2027, 1, 1);
        var contract = CreateContract(validTo: validTo);

        contract.Close(new DateOnly(2026, 8, 27));

        Assert.Equal(validTo, contract.ValidTo);
    }

    [Fact]
    public void Close_RejectsRepeatedClosure()
    {
        var contract = CreateContract();
        contract.Close(new DateOnly(2026, 8, 27));

        var exception = Assert.Throws<DomainRuleException>(
            () => contract.Close(new DateOnly(2026, 8, 28)));

        Assert.Equal(DomainErrorCodes.ContractAlreadyClosed, exception.Code);
    }

    [Fact]
    public void Close_RejectsContractWithUnfinishedCertification()
    {
        var contract = CreateContract();
        contract.AddProlongation(CreateProlongation(contract.Id, 1));

        var exception = Assert.Throws<DomainRuleException>(() =>
            contract.Close(new DateOnly(2026, 8, 27)));

        Assert.Equal(DomainErrorCodes.CertificationInProgressExists, exception.Code);
        Assert.True(contract.Active);
    }

    [Fact]
    public void AddProlongation_RejectsASecondInProgressCertification()
    {
        var contract = CreateContract();
        contract.AddProlongation(CreateProlongation(contract.Id, 1));

        var exception = Assert.Throws<DomainRuleException>(
            () => contract.AddProlongation(CreateProlongation(contract.Id, 2)));

        Assert.Equal(DomainErrorCodes.CertificationInProgressExists, exception.Code);
    }

    [Fact]
    public void AddProlongation_RejectsACertificationForAnotherContract()
    {
        var contract = CreateContract();

        var exception = Assert.Throws<DomainRuleException>(
            () => contract.AddProlongation(CreateProlongation(contract.Id + 1, 1)));

        Assert.Equal(DomainErrorCodes.CertificationContractMismatch, exception.Code);
    }

    [Fact]
    public void AddProlongation_RejectsAnInactiveContract()
    {
        var contract = CreateContract();
        contract.Close(new DateOnly(2026, 8, 27));

        var exception = Assert.Throws<DomainRuleException>(
            () => contract.AddProlongation(CreateProlongation(contract.Id, 1)));

        Assert.Equal(DomainErrorCodes.ContractInactive, exception.Code);
    }

    [Fact]
    public void CompleteProlongation_UpdatesValidToAndReturnsContractValid()
    {
        var contract = CreateContract(years: 2);
        var prolongation = CreateProlongation(contract.Id, 1);
        prolongation.Update(
            "Assessor",
            new DateOnly(2026, 4, 1),
            new DateOnly(2026, 4, 10),
            new DateOnly(2026, 4, 20));
        contract.AddProlongation(prolongation);

        var status = contract.CompleteProlongation(1, new DateOnly(2026, 4, 25));

        Assert.Equal(CertificationStatus.ContractValid, status);
        Assert.Equal(new DateOnly(2028, 4, 10), contract.ValidTo);
        Assert.Equal(new DateOnly(2026, 4, 25), prolongation.ProlongationReturned);
    }

    [Fact]
    public void AddProlongation_AllowsANewCycleAfterCompletion()
    {
        var contract = CreateContract();
        var first = CreateProlongation(contract.Id, 1);
        first.Update(
            "Assessor",
            new DateOnly(2026, 4, 1),
            new DateOnly(2026, 4, 2),
            new DateOnly(2026, 4, 3));
        contract.AddProlongation(first);
        contract.CompleteProlongation(first.Id, new DateOnly(2026, 4, 4));
        var second = Prolongation.Create(
            2,
            contract.Id,
            "Next assessor",
            new DateOnly(2027, 1, 1));

        contract.AddProlongation(second);

        Assert.Equal(2, contract.Prolongations.Count);
        Assert.False(second.IsCompleted);
    }

    private static Contract CreateContract(
        DateOnly? contractDate = null,
        DateOnly? validTo = null,
        int warningMonths = 3,
        int alertMonths = 1,
        int years = 1)
    {
        return Contract.Create(
            10,
            Guid.NewGuid(),
            "Engineer",
            contractDate ?? new DateOnly(2026, 1, 1),
            validTo: validTo,
            prolongationWarningMonths: warningMonths,
            prolongationAlertMonths: alertMonths,
            prolongationForYears: years);
    }

    private static Prolongation CreateProlongation(long contractId, long id)
    {
        return Prolongation.Create(
            id,
            contractId,
            "Assessor",
            new DateOnly(2026, 4, 1));
    }
}
