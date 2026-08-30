using Certifications.Application.Common;
using Certifications.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;

namespace Certifications.Api.Errors;

internal sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not ApplicationRuleException
            and not DomainRuleException
            and not BadHttpRequestException
            and not UnauthorizedAccessException)
        {
            logger.LogError(
                exception,
                "Unhandled exception while processing {Method} {Path}.",
                httpContext.Request.Method,
                httpContext.Request.Path);
        }

        IResult result = exception switch
        {
            RequestValidationException validation => Results.ValidationProblem(
                validation.Errors!.ToDictionary(pair => pair.Key, pair => pair.Value),
                statusCode: StatusCodes.Status400BadRequest,
                title: "Validation failed",
                extensions: Extensions(validation.Code, httpContext)),
            InvalidCredentialsException invalidCredentials => Problem(
                StatusCodes.Status401Unauthorized,
                "Authentication failed",
                invalidCredentials,
                httpContext),
            AccessDeniedException accessDenied => Problem(
                StatusCodes.Status403Forbidden,
                "Access denied",
                accessDenied,
                httpContext),
            ResourceNotFoundException notFound => Problem(
                StatusCodes.Status404NotFound,
                "Resource not found",
                notFound,
                httpContext),
            BusinessConflictException conflict => Problem(
                StatusCodes.Status409Conflict,
                "Business conflict",
                conflict,
                httpContext),
            DomainRuleException domain => Problem(
                IsConflict(domain.Code)
                    ? StatusCodes.Status409Conflict
                    : StatusCodes.Status400BadRequest,
                IsConflict(domain.Code) ? "Business conflict" : "Domain validation failed",
                domain.Code,
                domain.Message,
                httpContext),
            BadHttpRequestException badRequest => Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid request",
                detail: badRequest.Message,
                extensions: Extensions("request.invalid", httpContext)),
            UnauthorizedAccessException => Results.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Authentication failed",
                detail: "Authentication is required.",
                extensions: Extensions("auth.unauthorized", httpContext)),
            _ => Results.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Unexpected error",
                detail: "An unexpected error occurred.",
                extensions: Extensions("server.unexpected", httpContext))
        };

        await result.ExecuteAsync(httpContext);
        return true;
    }

    private static IResult Problem(
        int statusCode,
        string title,
        ApplicationRuleException exception,
        HttpContext context) =>
        Problem(statusCode, title, exception.Code, exception.Message, context);

    private static IResult Problem(
        int statusCode,
        string title,
        string code,
        string detail,
        HttpContext context) =>
        Results.Problem(
            statusCode: statusCode,
            title: title,
            detail: detail,
            extensions: Extensions(code, context));

    private static Dictionary<string, object?> Extensions(
        string code,
        HttpContext context) =>
        new()
        {
            ["code"] = code,
            ["traceId"] = context.TraceIdentifier
        };

    private static bool IsConflict(string code) => code is
        DomainErrorCodes.ActiveContractAlreadyExists
        or DomainErrorCodes.ContractAlreadyClosed
        or DomainErrorCodes.ContractInactive
        or DomainErrorCodes.CertificationInProgressExists
        or DomainErrorCodes.CertificationAlreadyCompleted;
}
