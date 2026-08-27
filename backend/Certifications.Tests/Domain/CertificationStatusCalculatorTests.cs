using Certifications.Domain.Entities;
using Certifications.Domain.Enums;
using Certifications.Domain.Services;

namespace Certifications.Tests.Domain;

public sealed class CertificationStatusCalculatorTests
{
    [Fact]
    public void Calculate_ReturnsNotApplicableWithoutAnActiveContract()
    {
        Assert.Equal(
            CertificationStatus.NotApplicable,
            CertificationStatusCalculator.Calculate(null, new DateOnly(2026, 1, 1)));

        var inactiveContract = CreateContract();
        inactiveContract.Close(new DateOnly(2026, 8, 1));

        Assert.Equal(
            CertificationStatus.NotApplicable,
            CertificationStatusCalculator.Calculate(inactiveContract, new DateOnly(2026, 8, 1)));
    }

    [Theory]
    [InlineData(2026, 9, 30, CertificationStatus.ContractValid)]
    [InlineData(2026, 10, 1, CertificationStatus.CertificationPending)]
    [InlineData(2026, 11, 30, CertificationStatus.CertificationPending)]
    [InlineData(2026, 12, 1, CertificationStatus.CertificationMissing)]
    [InlineData(2027, 1, 1, CertificationStatus.CertificationMissing)]
    public void Calculate_UsesInclusiveWarningAndAlertBoundaries(
        int year,
        int month,
        int day,
        CertificationStatus expected)
    {
        var contract = CreateContract();

        var status = CertificationStatusCalculator.Calculate(
            contract,
            new DateOnly(year, month, day));

        Assert.Equal(expected, status);
    }

    [Fact]
    public void Calculate_GivesInProgressCertificationHighestPriority()
    {
        var contract = CreateContract();
        contract.AddProlongation(Prolongation.Create(
            1,
            contract.Id,
            "Assessor",
            new DateOnly(2026, 12, 15)));

        var status = CertificationStatusCalculator.Calculate(
            contract,
            new DateOnly(2028, 1, 1));

        Assert.Equal(CertificationStatus.CertificationInProgress, status);
    }

    private static Contract CreateContract()
    {
        return Contract.Create(
            10,
            Guid.NewGuid(),
            "Engineer",
            new DateOnly(2026, 1, 1),
            validTo: new DateOnly(2027, 1, 1));
    }
}
