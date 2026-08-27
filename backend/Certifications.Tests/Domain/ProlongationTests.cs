using Certifications.Domain.Entities;
using Certifications.Domain.Exceptions;

namespace Certifications.Tests.Domain;

public sealed class ProlongationTests
{
    [Fact]
    public void Update_AllowsEqualDatesAcrossWorkflowStages()
    {
        var date = new DateOnly(2026, 4, 1);
        var prolongation = Prolongation.Create(1, 10, "Initial", date);

        prolongation.Update("Updated", date, date, date);

        Assert.Equal("Updated", prolongation.Assessor);
        Assert.Equal(date, prolongation.CertificationDate);
        Assert.Equal(date, prolongation.ProtocolDate);
        Assert.Equal(date, prolongation.ProlongationSend);
    }

    [Fact]
    public void Create_RequiresAnAssessor()
    {
        var exception = Assert.Throws<DomainRuleException>(() => Prolongation.Create(
            1,
            10,
            " ",
            new DateOnly(2026, 4, 1)));

        Assert.Equal(DomainErrorCodes.RequiredValue, exception.Code);
    }

    [Fact]
    public void Update_AllowsEarlierStagesToBeCorrectedBeforeCompletion()
    {
        var prolongation = CreateProlongation();
        prolongation.Update(
            "Assessor",
            new DateOnly(2026, 4, 1),
            new DateOnly(2026, 4, 2),
            new DateOnly(2026, 4, 3));

        prolongation.Update(
            "Corrected assessor",
            new DateOnly(2026, 3, 31),
            null,
            null);

        Assert.Equal("Corrected assessor", prolongation.Assessor);
        Assert.Equal(new DateOnly(2026, 3, 31), prolongation.CertificationDate);
        Assert.Null(prolongation.ProtocolDate);
        Assert.Null(prolongation.ProlongationSend);
    }

    [Fact]
    public void Update_RejectsASendDateWithoutAProtocolDate()
    {
        var prolongation = CreateProlongation();

        var exception = Assert.Throws<DomainRuleException>(() => prolongation.Update(
            "Assessor",
            new DateOnly(2026, 4, 1),
            null,
            new DateOnly(2026, 4, 2)));

        Assert.Equal(DomainErrorCodes.CertificationStageMissing, exception.Code);
    }

    [Fact]
    public void Update_RejectsDatesOutsideChronologicalOrder()
    {
        var prolongation = CreateProlongation();

        var exception = Assert.Throws<DomainRuleException>(() => prolongation.Update(
            "Assessor",
            new DateOnly(2026, 4, 2),
            new DateOnly(2026, 4, 1),
            new DateOnly(2026, 4, 3)));

        Assert.Equal(DomainErrorCodes.CertificationDateOrderInvalid, exception.Code);
    }

    [Fact]
    public void Complete_RequiresProtocolAndSendStages()
    {
        var contract = CreateContract();
        var prolongation = CreateProlongation(contract.Id);
        contract.AddProlongation(prolongation);

        var exception = Assert.Throws<DomainRuleException>(
            () => contract.CompleteProlongation(prolongation.Id, new DateOnly(2026, 4, 5)));

        Assert.Equal(DomainErrorCodes.CertificationStageMissing, exception.Code);
    }

    [Fact]
    public void Complete_RejectsAReturnDateBeforeTheSendDate()
    {
        var contract = CreateContract();
        var prolongation = CreateProlongation(contract.Id);
        prolongation.Update(
            "Assessor",
            new DateOnly(2026, 4, 1),
            new DateOnly(2026, 4, 2),
            new DateOnly(2026, 4, 4));
        contract.AddProlongation(prolongation);

        var exception = Assert.Throws<DomainRuleException>(
            () => contract.CompleteProlongation(prolongation.Id, new DateOnly(2026, 4, 3)));

        Assert.Equal(DomainErrorCodes.CertificationDateOrderInvalid, exception.Code);
    }

    [Fact]
    public void CompletedCertification_IsImmutable()
    {
        var contract = CreateContract();
        var prolongation = CreateProlongation(contract.Id);
        prolongation.Update(
            "Assessor",
            new DateOnly(2026, 4, 1),
            new DateOnly(2026, 4, 2),
            new DateOnly(2026, 4, 3));
        contract.AddProlongation(prolongation);
        contract.CompleteProlongation(prolongation.Id, new DateOnly(2026, 4, 4));

        var updateException = Assert.Throws<DomainRuleException>(() => prolongation.Update(
            "Changed",
            new DateOnly(2026, 4, 1),
            new DateOnly(2026, 4, 2),
            new DateOnly(2026, 4, 3)));
        var completionException = Assert.Throws<DomainRuleException>(
            () => contract.CompleteProlongation(prolongation.Id, new DateOnly(2026, 4, 5)));

        Assert.Equal(DomainErrorCodes.CertificationAlreadyCompleted, updateException.Code);
        Assert.Equal(DomainErrorCodes.CertificationAlreadyCompleted, completionException.Code);
    }

    private static Contract CreateContract()
    {
        return Contract.Create(
            10,
            Guid.NewGuid(),
            "Engineer",
            new DateOnly(2026, 1, 1));
    }

    private static Prolongation CreateProlongation(long contractId = 10)
    {
        return Prolongation.Create(
            1,
            contractId,
            "Assessor",
            new DateOnly(2026, 4, 1));
    }
}
