using Certifications.Domain.Entities;
using Certifications.Domain.Enums;

namespace Certifications.Domain.Services;

public static class CertificationStatusCalculator
{
    public static CertificationStatus Calculate(Contract? activeContract, DateOnly today)
    {
        if (activeContract is null || !activeContract.Active)
        {
            return CertificationStatus.NotApplicable;
        }

        if (activeContract.Prolongations.Any(prolongation => !prolongation.IsCompleted))
        {
            return CertificationStatus.CertificationInProgress;
        }

        var warningDate = activeContract.EffectiveValidTo.AddMonths(
            -activeContract.ProlongationWarningMonths);
        var alertDate = activeContract.EffectiveValidTo.AddMonths(
            -activeContract.ProlongationAlertMonths);

        if (today < warningDate)
        {
            return CertificationStatus.ContractValid;
        }

        if (today < alertDate)
        {
            return CertificationStatus.CertificationPending;
        }

        return CertificationStatus.CertificationMissing;
    }
}
