using Certifications.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Certifications.Infrastructure.Persistence;

public sealed class CertificationsDbContext(DbContextOptions<CertificationsDbContext> options)
    : DbContext(options)
{
    public DbSet<Employee> Employees => Set<Employee>();

    public DbSet<Contract> Contracts => Set<Contract>();

    public DbSet<Prolongation> Prolongations => Set<Prolongation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CertificationsDbContext).Assembly);
    }
}
