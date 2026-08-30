using Certifications.Application.Abstractions;
using Certifications.Application.Common;
using Certifications.Application.Contracts;
using Certifications.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Certifications.Application.Services;

public sealed class CertificationService(
    IApplicationDbContext dbContext,
    IBusinessClock clock)
{
    public async Task<IReadOnlyList<CertificationDto>> ListAsync(
        long contractId,
        CancellationToken cancellationToken)
    {
        if (!await dbContext.Contracts.AnyAsync(
                contract => contract.Id == contractId,
                cancellationToken))
        {
            throw ContractNotFound();
        }

        return await dbContext.Prolongations
            .AsNoTracking()
            .Where(item => item.ContractId == contractId)
            .OrderByDescending(item => item.CertificationDate)
            .ThenByDescending(item => item.Id)
            .Select(item => new CertificationDto(
                item.Id,
                item.ContractId,
                item.Assessor,
                item.CertificationDate,
                item.ProtocolDate,
                item.ProlongationSend,
                item.ProlongationReturned,
                item.ProlongationReturned != null))
            .ToListAsync(cancellationToken);
    }

    public async Task<CertificationDto> CreateAsync(
        long contractId,
        CreateCertificationRequest request,
        CancellationToken cancellationToken)
    {
        RequestValidator.Validate(request);
        var contract = await LoadContractAsync(contractId, cancellationToken);
        var certification = Prolongation.Create(
            0,
            contractId,
            request.Assessor,
            request.CertificationDate);
        contract.AddProlongation(certification);
        await dbContext.SaveChangesAsync(cancellationToken);
        return DtoMapper.ToCertification(certification);
    }

    public async Task<CertificationDto> UpdateAsync(
        long certificationId,
        UpdateCertificationRequest request,
        CancellationToken cancellationToken)
    {
        RequestValidator.Validate(request);
        var certification = await dbContext.Prolongations
            .SingleOrDefaultAsync(item => item.Id == certificationId, cancellationToken)
            ?? throw CertificationNotFound();
        var contractIsActive = await dbContext.Contracts.AnyAsync(
            contract => contract.Id == certification.ContractId && contract.Active,
            cancellationToken);

        if (!contractIsActive)
        {
            throw new BusinessConflictException(
                "contract.inactive",
                "Certifications on an inactive contract cannot be changed.");
        }

        certification.Update(
            request.Assessor,
            request.CertificationDate,
            request.ProtocolDate,
            request.ProlongationSend);
        await dbContext.SaveChangesAsync(cancellationToken);
        return DtoMapper.ToCertification(certification);
    }

    public async Task<ReturnCertificationResultDto> ReturnAsync(
        long certificationId,
        ReturnCertificationRequest request,
        CancellationToken cancellationToken)
    {
        var certification = await dbContext.Prolongations
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == certificationId, cancellationToken)
            ?? throw CertificationNotFound();
        RequestValidator.Validate(
            request,
            certification.CertificationDate,
            certification.ProtocolDate,
            certification.ProlongationSend);

        var contract = await LoadContractAsync(certification.ContractId, cancellationToken);
        dbContext.SetContractOriginalRowVersion(contract, request.RowVersion);
        contract.CompleteProlongation(certificationId, request.ProlongationReturned);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new BusinessConflictException(
                "contract.concurrency_conflict",
                "The contract was changed by another request.");
        }

        var completed = contract.Prolongations.Single(item => item.Id == certificationId);
        return new ReturnCertificationResultDto(
            DtoMapper.ToCertification(completed),
            DtoMapper.ToContract(contract, clock.Today));
    }

    private async Task<Contract> LoadContractAsync(
        long contractId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Contracts
            .Include(item => item.Prolongations)
            .SingleOrDefaultAsync(item => item.Id == contractId, cancellationToken)
            ?? throw ContractNotFound();
    }

    private static ResourceNotFoundException ContractNotFound() =>
        new("contract.not_found", "The contract was not found.");

    private static ResourceNotFoundException CertificationNotFound() =>
        new("certification.not_found", "The certification was not found.");
}
