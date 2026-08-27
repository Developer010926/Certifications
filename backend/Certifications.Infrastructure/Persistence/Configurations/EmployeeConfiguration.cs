using Certifications.Domain.Entities;
using Certifications.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Certifications.Infrastructure.Persistence.Configurations;

internal sealed class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable(
            "employees",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_employees_preferred_admin_mode_requires_admin",
                    "preferred_admin_mode IS NULL OR is_admin = TRUE");
                table.HasCheckConstraint(
                    "ck_employees_preferred_admin_mode_value",
                    "preferred_admin_mode IS NULL OR preferred_admin_mode IN ('MyPage', 'Administration')");
            });

        builder.HasKey(employee => employee.Id)
            .HasName("pk_employees");

        builder.Property(employee => employee.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(employee => employee.PersonalId)
            .HasColumnName("personal_id")
            .IsRequired();

        builder.Property(employee => employee.NormalizedPersonalId)
            .HasColumnName("normalized_personal_id")
            .IsRequired();

        builder.Property(employee => employee.FirstName)
            .HasColumnName("first_name")
            .IsRequired();

        builder.Property(employee => employee.MiddleName)
            .HasColumnName("middle_name");

        builder.Property(employee => employee.LastName)
            .HasColumnName("last_name")
            .IsRequired();

        builder.Property(employee => employee.EncryptedPassword)
            .HasColumnName("encrypted_password")
            .IsRequired();

        builder.Property(employee => employee.IsAdmin)
            .HasColumnName("is_admin");

        builder.Property(employee => employee.PreferredAdminMode)
            .HasColumnName("preferred_admin_mode")
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.HasIndex(employee => employee.NormalizedPersonalId)
            .IsUnique()
            .HasDatabaseName("ux_employees_normalized_personal_id");

        builder.HasMany(employee => employee.Contracts)
            .WithOne()
            .HasForeignKey(contract => contract.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_contracts_employees_employee_id");

        builder.Navigation(employee => employee.Contracts)
            .HasField("_contracts")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(employee => employee.ActiveContract);
    }
}
