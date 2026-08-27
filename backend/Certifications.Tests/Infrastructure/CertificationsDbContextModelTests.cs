using Certifications.Domain.Entities;
using Certifications.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Certifications.Tests.Infrastructure;

public sealed class CertificationsDbContextModelTests
{
    [Fact]
    public void EmployeeMapping_UsesExpectedSchemaAndStringEnum()
    {
        using var context = CreateContext();
        var entity = GetDesignTimeModel(context).FindEntityType(typeof(Employee));

        Assert.NotNull(entity);
        Assert.Equal("employees", entity.GetTableName());
        Assert.Null(entity.FindProperty(nameof(Employee.ActiveContract)));

        var preferredAdminMode = entity.FindProperty(nameof(Employee.PreferredAdminMode));
        Assert.NotNull(preferredAdminMode);
        Assert.Equal("preferred_admin_mode", preferredAdminMode.GetColumnName());
        Assert.Equal(
            typeof(string),
            preferredAdminMode.GetTypeMapping().Converter?.ProviderClrType);

        var normalizedPersonalIdIndex = Assert.Single(
            entity.GetIndexes(),
            index => index.GetDatabaseName() == "ux_employees_normalized_personal_id");
        Assert.True(normalizedPersonalIdIndex.IsUnique);

        var contracts = entity.FindNavigation(nameof(Employee.Contracts));
        Assert.NotNull(contracts);
        Assert.Equal(PropertyAccessMode.Field, contracts.GetPropertyAccessMode());
    }

    [Fact]
    public void ContractMapping_UsesXminAndActiveContractConstraint()
    {
        using var context = CreateContext();
        var entity = GetDesignTimeModel(context).FindEntityType(typeof(Contract));

        Assert.NotNull(entity);
        Assert.Equal("contracts", entity.GetTableName());
        Assert.Null(entity.FindProperty(nameof(Contract.EffectiveValidTo)));

        var rowVersion = entity.FindProperty(nameof(Contract.RowVersion));
        Assert.NotNull(rowVersion);
        Assert.Equal("xmin", rowVersion.GetColumnName());
        Assert.Equal("xid", rowVersion.GetColumnType());
        Assert.True(rowVersion.IsConcurrencyToken);
        Assert.Equal(ValueGenerated.OnAddOrUpdate, rowVersion.ValueGenerated);

        var activeContractIndex = Assert.Single(
            entity.GetIndexes(),
            index => index.GetDatabaseName() == "ux_contracts_employee_id_active");
        Assert.True(activeContractIndex.IsUnique);
        Assert.Equal("active = TRUE", activeContractIndex.GetFilter());

        var employeeForeignKey = Assert.Single(entity.GetForeignKeys());
        Assert.Equal(DeleteBehavior.Restrict, employeeForeignKey.DeleteBehavior);
    }

    [Fact]
    public void ProlongationMapping_UsesLatestAndInProgressIndexes()
    {
        using var context = CreateContext();
        var entity = GetDesignTimeModel(context).FindEntityType(typeof(Prolongation));

        Assert.NotNull(entity);
        Assert.Equal("prolongations", entity.GetTableName());
        Assert.Null(entity.FindProperty(nameof(Prolongation.IsCompleted)));

        var latestIndex = Assert.Single(
            entity.GetIndexes(),
            index => index.GetDatabaseName()
                == "ix_prolongations_contract_id_certification_date");
        Assert.Equal([false, true], latestIndex.IsDescending);

        var inProgressIndex = Assert.Single(
            entity.GetIndexes(),
            index => index.GetDatabaseName()
                == "ux_prolongations_contract_id_in_progress");
        Assert.True(inProgressIndex.IsUnique);
        Assert.Equal("prolongation_returned IS NULL", inProgressIndex.GetFilter());

        var contractForeignKey = Assert.Single(entity.GetForeignKeys());
        Assert.Equal(DeleteBehavior.Restrict, contractForeignKey.DeleteBehavior);
    }

    [Fact]
    public void Model_DefinesRequiredCheckConstraints()
    {
        using var context = CreateContext();
        var model = GetDesignTimeModel(context);

        Assert.Contains(
            model.FindEntityType(typeof(Employee))!.GetCheckConstraints(),
            constraint => constraint.Name
                == "ck_employees_preferred_admin_mode_requires_admin");
        Assert.Contains(
            model.FindEntityType(typeof(Contract))!.GetCheckConstraints(),
            constraint => constraint.Name == "ck_contracts_renewal_settings");
        Assert.Contains(
            model.FindEntityType(typeof(Prolongation))!.GetCheckConstraints(),
            constraint => constraint.Name == "ck_prolongations_date_sequence");
    }

    private static CertificationsDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<CertificationsDbContext>()
            .UseNpgsql()
            .Options;

        return new CertificationsDbContext(options);
    }

    private static IModel GetDesignTimeModel(CertificationsDbContext context)
    {
        return context.GetService<IDesignTimeModel>().Model;
    }
}
