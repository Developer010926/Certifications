using Certifications.Domain.Exceptions;

namespace Certifications.Domain.Services;

public static class PersonalIdNormalizer
{
    public static string Normalize(string? personalId)
    {
        if (personalId is null)
        {
            throw RequiredPersonalId();
        }

        var normalized = string.Concat(personalId.Where(character => !char.IsWhiteSpace(character)))
            .ToUpperInvariant();

        if (normalized.Length == 0)
        {
            throw RequiredPersonalId();
        }

        return normalized;
    }

    private static DomainRuleException RequiredPersonalId()
    {
        return new DomainRuleException(
            DomainErrorCodes.RequiredValue,
            "PersonalId is required.");
    }
}
