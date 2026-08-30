using Certifications.Domain.Enums;

namespace Certifications.Application.Contracts;

public sealed record LoginRequest(string PersonalId, string Password);

public sealed record PreferredModeRequest(AdminMode PreferredMode);

public sealed record CsrfTokenDto(string RequestToken);

public sealed record CurrentUserDto(
    Guid EmployeeId,
    string PersonalId,
    string DisplayName,
    bool IsAdmin,
    AdminMode? PreferredAdminMode);

public sealed record CreateEmployeeRequest(
    string PersonalId,
    string FirstName,
    string? MiddleName,
    string LastName,
    bool IsAdmin,
    CreateContractRequest FirstContract);

public sealed record UpdateEmployeeRequest(
    string PersonalId,
    string FirstName,
    string? MiddleName,
    string LastName,
    bool IsAdmin);

public sealed record EmployeeSummaryDto(
    Guid EmployeeId,
    string PersonalId,
    string FirstName,
    string? MiddleName,
    string LastName,
    bool IsAdmin,
    bool HasActiveContract,
    CertificationStatus Status);

public sealed record EmployeeDetailsDto(
    Guid EmployeeId,
    string PersonalId,
    string FirstName,
    string? MiddleName,
    string LastName,
    bool IsAdmin,
    AdminMode? PreferredAdminMode,
    ContractDetailsDto? CurrentContract);

public sealed record CreateEmployeeResultDto(
    EmployeeDetailsDto Employee,
    string GeneratedPassword);

public sealed record PasswordDto(string Password);

public sealed record CreateContractRequest(
    string Position,
    string? Department,
    string? Division,
    DateOnly ContractDate,
    DateOnly? ValidTo,
    int? ProlongationWarningMonths,
    int? ProlongationAlertMonths,
    int? ProlongationForYears);

public sealed record CloseContractRequest(DateOnly ClosedOn, uint RowVersion);

public sealed record ContractDto(
    long ContractId,
    Guid EmployeeId,
    string Position,
    string? Department,
    string? Division,
    DateOnly ContractDate,
    DateOnly? ValidTo,
    DateOnly EffectiveValidTo,
    bool Active,
    int ProlongationWarningMonths,
    int ProlongationAlertMonths,
    int ProlongationForYears,
    uint RowVersion,
    CertificationStatus Status);

public sealed record ContractDetailsDto(
    ContractDto Contract,
    IReadOnlyList<CertificationDto> Certifications);

public sealed record CreateCertificationRequest(
    string Assessor,
    DateOnly CertificationDate);

public sealed record UpdateCertificationRequest(
    string Assessor,
    DateOnly CertificationDate,
    DateOnly? ProtocolDate,
    DateOnly? ProlongationSend);

public sealed record ReturnCertificationRequest(
    DateOnly ProlongationReturned,
    uint RowVersion);

public sealed record CertificationDto(
    long CertificationId,
    long ContractId,
    string Assessor,
    DateOnly CertificationDate,
    DateOnly? ProtocolDate,
    DateOnly? ProlongationSend,
    DateOnly? ProlongationReturned,
    bool IsCompleted);

public sealed record ReturnCertificationResultDto(
    CertificationDto Certification,
    ContractDto Contract);

public sealed record CertificationOverviewRowDto(
    Guid EmployeeId,
    string PersonalId,
    string FirstName,
    string? MiddleName,
    string LastName,
    bool IsAdmin,
    long? ContractId,
    string? Position,
    string? Department,
    string? Division,
    DateOnly? ContractDate,
    DateOnly? EffectiveValidTo,
    CertificationStatus Status,
    CertificationDto? LatestCertification);

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount);

public sealed class EmployeeListQuery
{
    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 25;

    public string? Name { get; init; }

    public bool IncludeInactive { get; init; }

    public string Sort { get; init; } = "name";

    public string Direction { get; init; } = "asc";
}

public sealed class CertificationOverviewQuery
{
    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 25;

    public string? Name { get; init; }

    public string? Department { get; init; }

    public CertificationStatus? Status { get; init; }

    public DateOnly? ValidToFrom { get; init; }

    public DateOnly? ValidToTo { get; init; }

    public bool IncludeInactive { get; init; }

    public string Sort { get; init; } = "name";

    public string Direction { get; init; } = "asc";
}

public sealed record UserAccessDto(
    Guid EmployeeId,
    bool Exists,
    bool HasActiveContract,
    bool IsAdmin);
