using Certifications.Domain.Entities;
using Certifications.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Certifications.Infrastructure.Persistence.Configurations;

internal sealed class ContractConfiguration : IEntityTypeConfiguration<Contract>
{
    public void Configure(EntityTypeBuilder<Contract> builder)
    {
        builder.ToTable(
            "contracts",
            table => table.HasCheckConstraint(
                "ck_contracts_renewal_settings",
                "prolongation_warning_months >= 0"
                + " AND prolongation_alert_months >= 0"
                + " AND prolongation_for_years > 0"
                + " AND prolongation_alert_months < prolongation_warning_months"));

        builder.HasKey(contract => contract.Id)
            .HasName("pk_contracts");

        builder.Property(contract => contract.Id)
            .HasColumnName("id")
            .UseIdentityByDefaultColumn();

        builder.Property(contract => contract.EmployeeId)
            .HasColumnName("employee_id");

        builder.Property(contract => contract.Position)
            .HasColumnName("position")
            .IsRequired();

        builder.Property(contract => contract.Department)
            .HasColumnName("department");

        builder.Property(contract => contract.Division)
            .HasColumnName("division");

        builder.Property(contract => contract.ContractDate)
            .HasColumnName("contract_date")
            .HasColumnType("date");

        builder.Property(contract => contract.ValidTo)
            .HasColumnName("valid_to")
            .HasColumnType("date");

        builder.Property(contract => contract.Active)
            .HasColumnName("active");

        builder.Property(contract => contract.ProlongationWarningMonths)
            .HasColumnName("prolongation_warning_months")
            .HasDefaultValue(Contract.DefaultProlongationWarningMonths);

        builder.Property(contract => contract.ProlongationAlertMonths)
            .HasColumnName("prolongation_alert_months")
            .HasDefaultValue(Contract.DefaultProlongationAlertMonths);

        builder.Property(contract => contract.ProlongationForYears)
            .HasColumnName("prolongation_for_years")
            .HasDefaultValue(Contract.DefaultProlongationForYears);

        builder.Property(contract => contract.RowVersion)
            .HasColumnName("xmin")
            .IsRowVersion();

        builder.HasIndex(contract => contract.EmployeeId)
            .HasDatabaseName("ix_contracts_employee_id");

        builder.HasIndex(
                contract => contract.EmployeeId,
                "ux_contracts_employee_id_active_model")
            .IsUnique()
            .HasFilter("active = TRUE")
            .HasDatabaseName("ux_contracts_employee_id_active");

        builder.HasIndex(contract => contract.Department)
            .HasDatabaseName("ix_contracts_department");

        builder.HasIndex(contract => contract.ValidTo)
            .HasDatabaseName("ix_contracts_valid_to");

        builder.HasMany(contract => contract.Prolongations)
            .WithOne()
            .HasForeignKey(prolongation => prolongation.ContractId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_prolongations_contracts_contract_id");

        builder.Navigation(contract => contract.Prolongations)
            .HasField("_prolongations")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(contract => contract.EffectiveValidTo);

        builder.HasData(CriminalPoliceSeedData.Contracts);
    }
}
