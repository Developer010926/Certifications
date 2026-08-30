using Certifications.Domain.Enums;

namespace Certifications.Infrastructure.Persistence.Seed;

internal static class CriminalPoliceSeedData
{
    public const string Division = "Департамент криминальной полиции";
    public const string UnprovisionedPassword = "seed-data-password-not-provisioned";

    private static readonly Guid DirectorId = Guid.Parse("c0000000-0000-0000-0000-000000000001");
    private static readonly Guid InvestigationChiefId = Guid.Parse("c0000000-0000-0000-0000-000000000002");
    private static readonly Guid SeniorDetectiveId = Guid.Parse("c0000000-0000-0000-0000-000000000003");
    private static readonly Guid DetectiveId = Guid.Parse("c0000000-0000-0000-0000-000000000004");
    private static readonly Guid AnalystId = Guid.Parse("c0000000-0000-0000-0000-000000000005");
    private static readonly Guid InactiveDetectiveId = Guid.Parse("c0000000-0000-0000-0000-000000000006");

    public static IReadOnlyList<object> Employees { get; } =
    [
        new
        {
            Id = DirectorId,
            PersonalId = "КП-0001",
            NormalizedPersonalId = "КП-0001",
            FirstName = "Елена",
            MiddleName = "Сергеевна",
            LastName = "Волкова",
            EncryptedPassword = UnprovisionedPassword,
            IsAdmin = true,
            PreferredAdminMode = (AdminMode?)AdminMode.Administration
        },
        new
        {
            Id = InvestigationChiefId,
            PersonalId = "КП-0002",
            NormalizedPersonalId = "КП-0002",
            FirstName = "Алексей",
            MiddleName = "Николаевич",
            LastName = "Морозов",
            EncryptedPassword = UnprovisionedPassword,
            IsAdmin = false,
            PreferredAdminMode = (AdminMode?)null
        },
        new
        {
            Id = SeniorDetectiveId,
            PersonalId = "КП-0003",
            NormalizedPersonalId = "КП-0003",
            FirstName = "Мария",
            MiddleName = "Андреевна",
            LastName = "Кузнецова",
            EncryptedPassword = UnprovisionedPassword,
            IsAdmin = false,
            PreferredAdminMode = (AdminMode?)null
        },
        new
        {
            Id = DetectiveId,
            PersonalId = "КП-0004",
            NormalizedPersonalId = "КП-0004",
            FirstName = "Дмитрий",
            MiddleName = "Олегович",
            LastName = "Соколов",
            EncryptedPassword = UnprovisionedPassword,
            IsAdmin = false,
            PreferredAdminMode = (AdminMode?)null
        },
        new
        {
            Id = AnalystId,
            PersonalId = "КП-0005",
            NormalizedPersonalId = "КП-0005",
            FirstName = "Ирина",
            MiddleName = "Павловна",
            LastName = "Лебедева",
            EncryptedPassword = UnprovisionedPassword,
            IsAdmin = false,
            PreferredAdminMode = (AdminMode?)null
        },
        new
        {
            Id = InactiveDetectiveId,
            PersonalId = "КП-0006",
            NormalizedPersonalId = "КП-0006",
            FirstName = "Николай",
            MiddleName = "Евгеньевич",
            LastName = "Фёдоров",
            EncryptedPassword = UnprovisionedPassword,
            IsAdmin = false,
            PreferredAdminMode = (AdminMode?)null
        }
    ];

    public static IReadOnlyList<object> Contracts { get; } =
    [
        CreateContract(
            -1001,
            DirectorId,
            "Начальник департамента",
            "Руководство департамента",
            new DateOnly(2024, 1, 15),
            new DateOnly(2027, 12, 31)),
        CreateContract(
            -1002,
            InvestigationChiefId,
            "Начальник отдела",
            "Отдел уголовного розыска",
            new DateOnly(2023, 11, 15),
            new DateOnly(2026, 11, 15)),
        CreateContract(
            -1003,
            SeniorDetectiveId,
            "Старший оперуполномоченный",
            "Отдел уголовного розыска",
            new DateOnly(2023, 9, 15),
            new DateOnly(2026, 9, 15)),
        CreateContract(
            -1004,
            DetectiveId,
            "Оперуполномоченный",
            "Отдел уголовного розыска",
            new DateOnly(2023, 8, 1),
            new DateOnly(2026, 8, 1)),
        CreateContract(
            -1005,
            AnalystId,
            "Криминальный аналитик",
            "Отдел криминального анализа",
            new DateOnly(2024, 7, 5),
            new DateOnly(2027, 7, 5)),
        CreateContract(
            -1006,
            InactiveDetectiveId,
            "Оперуполномоченный",
            "Отдел уголовного розыска",
            new DateOnly(2022, 1, 10),
            new DateOnly(2025, 12, 31),
            active: false)
    ];

    public static IReadOnlyList<object> Prolongations { get; } =
    [
        new
        {
            Id = -2001L,
            ContractId = -1004L,
            Assessor = "полковник полиции Виктор Петрович Громов",
            CertificationDate = new DateOnly(2026, 8, 10),
            ProtocolDate = (DateOnly?)null,
            ProlongationSend = (DateOnly?)null,
            ProlongationReturned = (DateOnly?)null
        },
        new
        {
            Id = -2002L,
            ContractId = -1005L,
            Assessor = "полковник полиции Виктор Петрович Громов",
            CertificationDate = new DateOnly(2026, 7, 1),
            ProtocolDate = (DateOnly?)new DateOnly(2026, 7, 5),
            ProlongationSend = (DateOnly?)new DateOnly(2026, 7, 10),
            ProlongationReturned = (DateOnly?)new DateOnly(2026, 7, 20)
        }
    ];

    private static object CreateContract(
        long id,
        Guid employeeId,
        string position,
        string department,
        DateOnly contractDate,
        DateOnly validTo,
        bool active = true)
    {
        return new
        {
            Id = id,
            EmployeeId = employeeId,
            Position = position,
            Department = department,
            Division,
            ContractDate = contractDate,
            ValidTo = (DateOnly?)validTo,
            Active = active,
            ProlongationWarningMonths = 3,
            ProlongationAlertMonths = 1,
            ProlongationForYears = 1
        };
    }
}
