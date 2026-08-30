using Certifications.Domain.Entities;
using Certifications.Domain.Enums;
using Certifications.Domain.Services;
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

    [Fact]
    public void Model_SeedsRepresentativeCriminalPoliceDepartment()
    {
        using var context = CreateContext();
        var model = GetDesignTimeModel(context);
        var employeeSeed = model.FindEntityType(typeof(Employee))!.GetSeedData().ToArray();
        var contractSeed = model.FindEntityType(typeof(Contract))!.GetSeedData().ToArray();
        var prolongationSeed = model.FindEntityType(typeof(Prolongation))!.GetSeedData().ToArray();

        Assert.Equal(6, employeeSeed.Length);
        Assert.Equal(6, contractSeed.Length);
        Assert.Equal(2, prolongationSeed.Length);

        Assert.All(
            employeeSeed,
            employee =>
            {
                Assert.Equal(
                    employee[nameof(Employee.PersonalId)],
                    employee[nameof(Employee.NormalizedPersonalId)]);
                Assert.Equal(
                    "seed-data-password-not-provisioned",
                    employee[nameof(Employee.EncryptedPassword)]);
            });

        var administrator = Assert.Single(
            employeeSeed,
            employee => (bool)employee[nameof(Employee.IsAdmin)]!);
        Assert.Equal(
            AdminMode.Administration,
            administrator[nameof(Employee.PreferredAdminMode)]);

        var employeeIds = employeeSeed
            .Select(employee => (Guid)employee[nameof(Employee.Id)]!)
            .ToHashSet();

        Assert.All(
            contractSeed,
            contract =>
            {
                Assert.True((long)contract[nameof(Contract.Id)]! < 0);
                Assert.Contains((Guid)contract[nameof(Contract.EmployeeId)]!, employeeIds);
                Assert.Equal(
                    "Департамент криминальной полиции",
                    contract[nameof(Contract.Division)]);
            });

        var contractIds = contractSeed
            .Select(contract => (long)contract[nameof(Contract.Id)]!)
            .ToHashSet();

        Assert.All(
            prolongationSeed,
            prolongation =>
            {
                Assert.True((long)prolongation[nameof(Prolongation.Id)]! < 0);
                Assert.Contains(
                    (long)prolongation[nameof(Prolongation.ContractId)]!,
                    contractIds);
                AssertDateSequence(prolongation);
            });

        var personalIdsByEmployeeId = employeeSeed.ToDictionary(
            employee => (Guid)employee[nameof(Employee.Id)]!,
            employee => (string)employee[nameof(Employee.PersonalId)]!);
        var statusesByPersonalId = contractSeed.ToDictionary(
            contract => personalIdsByEmployeeId[(Guid)contract[nameof(Contract.EmployeeId)]!],
            contract => CertificationStatusCalculator.Calculate(
                CreateContract(contract, prolongationSeed),
                new DateOnly(2026, 8, 27)));

        Assert.Equal(CertificationStatus.ContractValid, statusesByPersonalId["КП-0001"]);
        Assert.Equal(CertificationStatus.CertificationPending, statusesByPersonalId["КП-0002"]);
        Assert.Equal(CertificationStatus.CertificationMissing, statusesByPersonalId["КП-0003"]);
        Assert.Equal(CertificationStatus.CertificationInProgress, statusesByPersonalId["КП-0004"]);
        Assert.Equal(CertificationStatus.ContractValid, statusesByPersonalId["КП-0005"]);
        Assert.Equal(CertificationStatus.NotApplicable, statusesByPersonalId["КП-0006"]);
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

    private static Contract CreateContract(
        IDictionary<string, object?> contractSeed,
        IEnumerable<IDictionary<string, object?>> prolongationSeed)
    {
        var contract = Contract.Create(
            (long)contractSeed[nameof(Contract.Id)]!,
            (Guid)contractSeed[nameof(Contract.EmployeeId)]!,
            (string)contractSeed[nameof(Contract.Position)]!,
            (DateOnly)contractSeed[nameof(Contract.ContractDate)]!,
            (string?)contractSeed[nameof(Contract.Department)],
            (string?)contractSeed[nameof(Contract.Division)],
            (DateOnly?)contractSeed[nameof(Contract.ValidTo)],
            (int)contractSeed[nameof(Contract.ProlongationWarningMonths)]!,
            (int)contractSeed[nameof(Contract.ProlongationAlertMonths)]!,
            (int)contractSeed[nameof(Contract.ProlongationForYears)]!);

        if (!(bool)contractSeed[nameof(Contract.Active)]!)
        {
            contract.Close((DateOnly)contractSeed[nameof(Contract.ValidTo)]!);
            return contract;
        }

        foreach (var seed in prolongationSeed.Where(
                     item => (long)item[nameof(Prolongation.ContractId)]! == contract.Id))
        {
            var prolongation = Prolongation.Create(
                (long)seed[nameof(Prolongation.Id)]!,
                contract.Id,
                (string)seed[nameof(Prolongation.Assessor)]!,
                (DateOnly)seed[nameof(Prolongation.CertificationDate)]!);
            var protocolDate = (DateOnly?)seed[nameof(Prolongation.ProtocolDate)];
            var prolongationSend = (DateOnly?)seed[nameof(Prolongation.ProlongationSend)];
            var prolongationReturned = (DateOnly?)seed[nameof(Prolongation.ProlongationReturned)];

            prolongation.Update(
                prolongation.Assessor,
                prolongation.CertificationDate,
                protocolDate,
                prolongationSend);
            contract.AddProlongation(prolongation);

            if (prolongationReturned.HasValue)
            {
                contract.CompleteProlongation(prolongation.Id, prolongationReturned.Value);
            }
        }

        return contract;
    }

    private static void AssertDateSequence(IDictionary<string, object?> prolongation)
    {
        var certificationDate = (DateOnly)prolongation[nameof(Prolongation.CertificationDate)]!;
        var protocolDate = (DateOnly?)prolongation[nameof(Prolongation.ProtocolDate)];
        var prolongationSend = (DateOnly?)prolongation[nameof(Prolongation.ProlongationSend)];
        var prolongationReturned = (DateOnly?)prolongation[nameof(Prolongation.ProlongationReturned)];

        Assert.True(!protocolDate.HasValue || protocolDate.Value >= certificationDate);
        Assert.True(!prolongationSend.HasValue
            || protocolDate.HasValue && prolongationSend.Value >= protocolDate.Value);
        Assert.True(!prolongationReturned.HasValue
            || prolongationSend.HasValue && prolongationReturned.Value >= prolongationSend.Value);
    }
}
