using Certifications.Domain.Exceptions;
using Certifications.Domain.Services;

namespace Certifications.Tests.Domain;

public sealed class PersonalIdNormalizerTests
{
    [Theory]
    [InlineData(" emp 001 ", "EMP001")]
    [InlineData("e\tm\np001", "EMP001")]
    [InlineData("e\u00A0mp001", "EMP001")]
    public void Normalize_RemovesWhitespaceAndUsesInvariantUppercase(
        string personalId,
        string expected)
    {
        var result = PersonalIdNormalizer.Normalize(personalId);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t\n")]
    public void Normalize_RejectsMissingValues(string? personalId)
    {
        var exception = Assert.Throws<DomainRuleException>(
            () => PersonalIdNormalizer.Normalize(personalId));

        Assert.Equal(DomainErrorCodes.RequiredValue, exception.Code);
    }
}
