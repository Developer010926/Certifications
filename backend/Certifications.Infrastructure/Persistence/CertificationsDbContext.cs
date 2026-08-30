using Certifications.Application.Abstractions;
using Certifications.Application.Common;
using Certifications.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Certifications.Infrastructure.Persistence;

public sealed class CertificationsDbContext(DbContextOptions<CertificationsDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<Employee> Employees => Set<Employee>();

    public DbSet<Contract> Contracts => Set<Contract>();

    public DbSet<Prolongation> Prolongations => Set<Prolongation>();

    public void SetContractOriginalRowVersion(Contract contract, uint rowVersion)
    {
        Entry(contract).Property(item => item.RowVersion).OriginalValue = rowVersion;
    }

    public override async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException postgresException
                && postgresException.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw postgresException.ConstraintName switch
            {
                "ux_employees_normalized_personal_id" => new BusinessConflictException(
                    "employee.personal_id_conflict",
                    "The normalized personal ID is already in use."),
                "ux_contracts_employee_id_active" => new BusinessConflictException(
                    "contract.active_already_exists",
                    "The employee already has an active contract."),
                "ux_prolongations_contract_id_in_progress" => new BusinessConflictException(
                    "certification.in_progress_exists",
                    "The contract already has an unfinished certification."),
                _ => new BusinessConflictException(
                    "persistence.unique_conflict",
                    "A unique database constraint was violated.")
            };
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CertificationsDbContext).Assembly);
    }
}
