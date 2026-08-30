using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Certifications.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedCriminalPoliceDepartment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "employees",
                columns: new[] { "id", "encrypted_password", "first_name", "is_admin", "last_name", "middle_name", "normalized_personal_id", "personal_id", "preferred_admin_mode" },
                values: new object[,]
                {
                    { new Guid("c0000000-0000-0000-0000-000000000001"), "seed-data-password-not-provisioned", "Елена", true, "Волкова", "Сергеевна", "КП-0001", "КП-0001", "Administration" },
                    { new Guid("c0000000-0000-0000-0000-000000000002"), "seed-data-password-not-provisioned", "Алексей", false, "Морозов", "Николаевич", "КП-0002", "КП-0002", null },
                    { new Guid("c0000000-0000-0000-0000-000000000003"), "seed-data-password-not-provisioned", "Мария", false, "Кузнецова", "Андреевна", "КП-0003", "КП-0003", null },
                    { new Guid("c0000000-0000-0000-0000-000000000004"), "seed-data-password-not-provisioned", "Дмитрий", false, "Соколов", "Олегович", "КП-0004", "КП-0004", null },
                    { new Guid("c0000000-0000-0000-0000-000000000005"), "seed-data-password-not-provisioned", "Ирина", false, "Лебедева", "Павловна", "КП-0005", "КП-0005", null },
                    { new Guid("c0000000-0000-0000-0000-000000000006"), "seed-data-password-not-provisioned", "Николай", false, "Фёдоров", "Евгеньевич", "КП-0006", "КП-0006", null }
                });

            migrationBuilder.InsertData(
                table: "contracts",
                columns: new[] { "id", "active", "contract_date", "department", "division", "employee_id", "position", "prolongation_alert_months", "prolongation_for_years", "prolongation_warning_months", "valid_to" },
                values: new object[,]
                {
                    { -1006L, false, new DateOnly(2022, 1, 10), "Отдел уголовного розыска", "Департамент криминальной полиции", new Guid("c0000000-0000-0000-0000-000000000006"), "Оперуполномоченный", 1, 1, 3, new DateOnly(2025, 12, 31) },
                    { -1005L, true, new DateOnly(2024, 7, 5), "Отдел криминального анализа", "Департамент криминальной полиции", new Guid("c0000000-0000-0000-0000-000000000005"), "Криминальный аналитик", 1, 1, 3, new DateOnly(2027, 7, 5) },
                    { -1004L, true, new DateOnly(2023, 8, 1), "Отдел уголовного розыска", "Департамент криминальной полиции", new Guid("c0000000-0000-0000-0000-000000000004"), "Оперуполномоченный", 1, 1, 3, new DateOnly(2026, 8, 1) },
                    { -1003L, true, new DateOnly(2023, 9, 15), "Отдел уголовного розыска", "Департамент криминальной полиции", new Guid("c0000000-0000-0000-0000-000000000003"), "Старший оперуполномоченный", 1, 1, 3, new DateOnly(2026, 9, 15) },
                    { -1002L, true, new DateOnly(2023, 11, 15), "Отдел уголовного розыска", "Департамент криминальной полиции", new Guid("c0000000-0000-0000-0000-000000000002"), "Начальник отдела", 1, 1, 3, new DateOnly(2026, 11, 15) },
                    { -1001L, true, new DateOnly(2024, 1, 15), "Руководство департамента", "Департамент криминальной полиции", new Guid("c0000000-0000-0000-0000-000000000001"), "Начальник департамента", 1, 1, 3, new DateOnly(2027, 12, 31) }
                });

            migrationBuilder.InsertData(
                table: "prolongations",
                columns: new[] { "id", "assessor", "certification_date", "contract_id", "prolongation_returned", "prolongation_send", "protocol_date" },
                values: new object[,]
                {
                    { -2002L, "полковник полиции Виктор Петрович Громов", new DateOnly(2026, 7, 1), -1005L, new DateOnly(2026, 7, 20), new DateOnly(2026, 7, 10), new DateOnly(2026, 7, 5) },
                    { -2001L, "полковник полиции Виктор Петрович Громов", new DateOnly(2026, 8, 10), -1004L, null, null, null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "contracts",
                keyColumn: "id",
                keyValue: -1006L);

            migrationBuilder.DeleteData(
                table: "contracts",
                keyColumn: "id",
                keyValue: -1003L);

            migrationBuilder.DeleteData(
                table: "contracts",
                keyColumn: "id",
                keyValue: -1002L);

            migrationBuilder.DeleteData(
                table: "contracts",
                keyColumn: "id",
                keyValue: -1001L);

            migrationBuilder.DeleteData(
                table: "prolongations",
                keyColumn: "id",
                keyValue: -2002L);

            migrationBuilder.DeleteData(
                table: "prolongations",
                keyColumn: "id",
                keyValue: -2001L);

            migrationBuilder.DeleteData(
                table: "contracts",
                keyColumn: "id",
                keyValue: -1005L);

            migrationBuilder.DeleteData(
                table: "contracts",
                keyColumn: "id",
                keyValue: -1004L);

            migrationBuilder.DeleteData(
                table: "employees",
                keyColumn: "id",
                keyValue: new Guid("c0000000-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "employees",
                keyColumn: "id",
                keyValue: new Guid("c0000000-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "employees",
                keyColumn: "id",
                keyValue: new Guid("c0000000-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                table: "employees",
                keyColumn: "id",
                keyValue: new Guid("c0000000-0000-0000-0000-000000000006"));

            migrationBuilder.DeleteData(
                table: "employees",
                keyColumn: "id",
                keyValue: new Guid("c0000000-0000-0000-0000-000000000004"));

            migrationBuilder.DeleteData(
                table: "employees",
                keyColumn: "id",
                keyValue: new Guid("c0000000-0000-0000-0000-000000000005"));
        }
    }
}
