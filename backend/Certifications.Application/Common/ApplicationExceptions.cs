namespace Certifications.Application.Common;

public abstract class ApplicationRuleException(
    string code,
    string message,
    IReadOnlyDictionary<string, string[]>? errors = null)
    : Exception(message)
{
    public string Code { get; } = code;

    public IReadOnlyDictionary<string, string[]>? Errors { get; } = errors;
}

public sealed class RequestValidationException(
    IReadOnlyDictionary<string, string[]> errors)
    : ApplicationRuleException(
        "request.validation_failed",
        "One or more validation errors occurred.",
        errors);

public sealed class ResourceNotFoundException(string code, string message)
    : ApplicationRuleException(code, message);

public sealed class BusinessConflictException(string code, string message)
    : ApplicationRuleException(code, message);

public sealed class AccessDeniedException(string code, string message)
    : ApplicationRuleException(code, message);

public sealed class InvalidCredentialsException()
    : ApplicationRuleException(
        "auth.invalid_credentials",
        "The credentials are invalid or login is not allowed.");
