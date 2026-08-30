using Certifications.Application.Contracts;

namespace Certifications.Application.Common;

internal static class RequestValidator
{
    public static void Validate(LoginRequest request)
    {
        var errors = NewErrors();
        Required(errors, nameof(request.PersonalId), request.PersonalId);
        Required(errors, nameof(request.Password), request.Password);
        ThrowIfAny(errors);
    }

    public static void Validate(CreateEmployeeRequest request)
    {
        var errors = NewErrors();
        Required(errors, nameof(request.PersonalId), request.PersonalId);
        Required(errors, nameof(request.FirstName), request.FirstName);
        Required(errors, nameof(request.LastName), request.LastName);

        if (request.FirstContract is null)
        {
            Add(errors, nameof(request.FirstContract), "First contract is required.");
        }
        else
        {
            ValidateContract(request.FirstContract, errors, "firstContract.");
        }

        ThrowIfAny(errors);
    }

    public static void Validate(UpdateEmployeeRequest request)
    {
        var errors = NewErrors();
        Required(errors, nameof(request.PersonalId), request.PersonalId);
        Required(errors, nameof(request.FirstName), request.FirstName);
        Required(errors, nameof(request.LastName), request.LastName);
        ThrowIfAny(errors);
    }

    public static void Validate(CreateContractRequest request)
    {
        var errors = NewErrors();
        ValidateContract(request, errors, string.Empty);
        ThrowIfAny(errors);
    }

    public static void Validate(CreateCertificationRequest request)
    {
        var errors = NewErrors();
        Required(errors, nameof(request.Assessor), request.Assessor);
        ThrowIfAny(errors);
    }

    public static void Validate(UpdateCertificationRequest request)
    {
        var errors = NewErrors();
        Required(errors, nameof(request.Assessor), request.Assessor);
        ValidateCertificationDates(
            errors,
            request.CertificationDate,
            request.ProtocolDate,
            request.ProlongationSend,
            null);
        ThrowIfAny(errors);
    }

    public static void Validate(ReturnCertificationRequest request, DateOnly certificationDate, DateOnly? protocolDate, DateOnly? sendDate)
    {
        var errors = NewErrors();
        ValidateCertificationDates(
            errors,
            certificationDate,
            protocolDate,
            sendDate,
            request.ProlongationReturned);
        ThrowIfAny(errors);
    }

    public static void ValidatePagination(int page, int pageSize)
    {
        var errors = NewErrors();

        if (page < 1)
        {
            Add(errors, "page", "Page must be at least 1.");
        }

        if (pageSize is < 1 or > 100)
        {
            Add(errors, "pageSize", "Page size must be between 1 and 100.");
        }

        ThrowIfAny(errors);
    }

    public static void ValidateSort(string sort, string direction, params string[] allowedSorts)
    {
        var errors = NewErrors();

        if (!allowedSorts.Contains(sort, StringComparer.OrdinalIgnoreCase))
        {
            Add(errors, "sort", $"Sort must be one of: {string.Join(", ", allowedSorts)}.");
        }

        if (!direction.Equals("asc", StringComparison.OrdinalIgnoreCase)
            && !direction.Equals("desc", StringComparison.OrdinalIgnoreCase))
        {
            Add(errors, "direction", "Direction must be 'asc' or 'desc'.");
        }

        ThrowIfAny(errors);
    }

    private static void ValidateContract(
        CreateContractRequest request,
        Dictionary<string, List<string>> errors,
        string prefix)
    {
        Required(errors, prefix + "position", request.Position);

        var warning = request.ProlongationWarningMonths ?? 3;
        var alert = request.ProlongationAlertMonths ?? 1;
        var years = request.ProlongationForYears ?? 1;

        if (warning < 0)
        {
            Add(errors, prefix + "prolongationWarningMonths", "Warning months cannot be negative.");
        }

        if (alert < 0)
        {
            Add(errors, prefix + "prolongationAlertMonths", "Alert months cannot be negative.");
        }

        if (years <= 0)
        {
            Add(errors, prefix + "prolongationForYears", "Prolongation years must be positive.");
        }

        if (alert >= warning)
        {
            Add(errors, prefix + "prolongationAlertMonths", "Alert months must be below warning months.");
        }
    }

    private static void ValidateCertificationDates(
        Dictionary<string, List<string>> errors,
        DateOnly certificationDate,
        DateOnly? protocolDate,
        DateOnly? sendDate,
        DateOnly? returnedDate)
    {
        if (sendDate.HasValue && !protocolDate.HasValue)
        {
            Add(errors, "prolongationSend", "Protocol date is required before the send date.");
        }

        if (returnedDate.HasValue && !sendDate.HasValue)
        {
            Add(errors, "prolongationReturned", "Send date is required before the returned date.");
        }

        if (protocolDate < certificationDate)
        {
            Add(errors, "protocolDate", "Protocol date cannot precede certification date.");
        }

        if (sendDate < protocolDate)
        {
            Add(errors, "prolongationSend", "Send date cannot precede protocol date.");
        }

        if (returnedDate < sendDate)
        {
            Add(errors, "prolongationReturned", "Returned date cannot precede send date.");
        }
    }

    private static Dictionary<string, List<string>> NewErrors() =>
        new(StringComparer.OrdinalIgnoreCase);

    private static void Required(
        Dictionary<string, List<string>> errors,
        string field,
        string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            Add(errors, field, "The value is required.");
        }
    }

    private static void Add(
        Dictionary<string, List<string>> errors,
        string field,
        string message)
    {
        if (!errors.TryGetValue(field, out var messages))
        {
            messages = [];
            errors[field] = messages;
        }

        messages.Add(message);
    }

    private static void ThrowIfAny(Dictionary<string, List<string>> errors)
    {
        if (errors.Count == 0)
        {
            return;
        }

        throw new RequestValidationException(
            errors.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray()));
    }
}
