using Certifications.Domain.Exceptions;
using Certifications.Domain.Internal;

namespace Certifications.Domain.Entities;

public sealed class Prolongation
{
    private Prolongation()
    {
        Assessor = null!;
    }

    private Prolongation(
        long id,
        long contractId,
        string assessor,
        DateOnly certificationDate)
    {
        Id = id;
        ContractId = contractId;
        Assessor = DomainGuard.Required(assessor, nameof(Assessor));
        CertificationDate = certificationDate;
    }

    public long Id { get; private set; }

    public long ContractId { get; private set; }

    public string Assessor { get; private set; }

    public DateOnly CertificationDate { get; private set; }

    public DateOnly? ProtocolDate { get; private set; }

    public DateOnly? ProlongationSend { get; private set; }

    public DateOnly? ProlongationReturned { get; private set; }

    public bool IsCompleted => ProlongationReturned.HasValue;

    public static Prolongation Create(
        long id,
        long contractId,
        string assessor,
        DateOnly certificationDate)
    {
        return new Prolongation(id, contractId, assessor, certificationDate);
    }

    public void Update(
        string assessor,
        DateOnly certificationDate,
        DateOnly? protocolDate,
        DateOnly? prolongationSend)
    {
        EnsureNotCompleted();
        ValidateDateSequence(certificationDate, protocolDate, prolongationSend, null);

        Assessor = DomainGuard.Required(assessor, nameof(Assessor));
        CertificationDate = certificationDate;
        ProtocolDate = protocolDate;
        ProlongationSend = prolongationSend;
    }

    internal void Complete(DateOnly returnedDate)
    {
        EnsureNotCompleted();

        if (!ProtocolDate.HasValue || !ProlongationSend.HasValue)
        {
            throw new DomainRuleException(
                DomainErrorCodes.CertificationStageMissing,
                "ProtocolDate and ProlongationSend are required before completing a certification.");
        }

        ValidateDateSequence(
            CertificationDate,
            ProtocolDate,
            ProlongationSend,
            returnedDate);

        ProlongationReturned = returnedDate;
    }

    private static void ValidateDateSequence(
        DateOnly certificationDate,
        DateOnly? protocolDate,
        DateOnly? prolongationSend,
        DateOnly? prolongationReturned)
    {
        if (prolongationSend.HasValue && !protocolDate.HasValue
            || prolongationReturned.HasValue && !prolongationSend.HasValue)
        {
            throw new DomainRuleException(
                DomainErrorCodes.CertificationStageMissing,
                "A certification stage cannot be set before its preceding stage.");
        }

        if (protocolDate < certificationDate
            || prolongationSend < protocolDate
            || prolongationReturned < prolongationSend)
        {
            throw new DomainRuleException(
                DomainErrorCodes.CertificationDateOrderInvalid,
                "Certification dates must follow their required chronological order.");
        }
    }

    private void EnsureNotCompleted()
    {
        if (IsCompleted)
        {
            throw new DomainRuleException(
                DomainErrorCodes.CertificationAlreadyCompleted,
                "A completed certification cannot be changed.");
        }
    }
}
