using Certifications.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Certifications.Application.Abstractions;

public interface IApplicationDbContext
{
    DbSet<Employee> Employees { get; }

    DbSet<Contract> Contracts { get; }

    DbSet<Prolongation> Prolongations { get; }

    void SetContractOriginalRowVersion(Contract contract, uint rowVersion);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
