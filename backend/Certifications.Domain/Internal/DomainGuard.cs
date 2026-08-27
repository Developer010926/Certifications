using Certifications.Domain.Exceptions;

namespace Certifications.Domain.Internal;

internal static class DomainGuard
{
    public static string Required(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainRuleException(
                DomainErrorCodes.RequiredValue,
                $"{fieldName} is required.");
        }

        return value;
    }
}
