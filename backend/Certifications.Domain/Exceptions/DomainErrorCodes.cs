namespace Certifications.Domain.Exceptions;

public static class DomainErrorCodes
{
    public const string RequiredValue = "domain.required_value";
    public const string InvalidEmployeeId = "employee.invalid_id";
    public const string FirstContractRequired = "employee.first_contract_required";
    public const string ContractEmployeeMismatch = "contract.employee_mismatch";
    public const string ContractMustBeActive = "contract.must_be_active";
    public const string ActiveContractAlreadyExists = "contract.active_already_exists";
    public const string ContractAlreadyClosed = "contract.already_closed";
    public const string InvalidRenewalSettings = "contract.invalid_renewal_settings";
    public const string ContractInactive = "contract.inactive";
    public const string CertificationContractMismatch = "certification.contract_mismatch";
    public const string CertificationInProgressExists = "certification.in_progress_exists";
    public const string CertificationNotFound = "certification.not_found";
    public const string CertificationAlreadyCompleted = "certification.already_completed";
    public const string CertificationStageMissing = "certification.stage_missing";
    public const string CertificationDateOrderInvalid = "certification.date_order_invalid";
    public const string AdminModeRequiresAdmin = "employee.admin_mode_requires_admin";
}
