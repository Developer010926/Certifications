using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Certifications.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "employees",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    personal_id = table.Column<string>(type: "text", nullable: false),
                    normalized_personal_id = table.Column<string>(type: "text", nullable: false),
                    first_name = table.Column<string>(type: "text", nullable: false),
                    middle_name = table.Column<string>(type: "text", nullable: true),
                    last_name = table.Column<string>(type: "text", nullable: false),
                    encrypted_password = table.Column<string>(type: "text", nullable: false),
                    is_admin = table.Column<bool>(type: "boolean", nullable: false),
                    preferred_admin_mode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_employees", x => x.id);
                    table.CheckConstraint("ck_employees_preferred_admin_mode_requires_admin", "preferred_admin_mode IS NULL OR is_admin = TRUE");
                    table.CheckConstraint("ck_employees_preferred_admin_mode_value", "preferred_admin_mode IS NULL OR preferred_admin_mode IN ('MyPage', 'Administration')");
                });

            migrationBuilder.CreateTable(
                name: "contracts",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    position = table.Column<string>(type: "text", nullable: false),
                    department = table.Column<string>(type: "text", nullable: true),
                    division = table.Column<string>(type: "text", nullable: true),
                    contract_date = table.Column<DateOnly>(type: "date", nullable: false),
                    valid_to = table.Column<DateOnly>(type: "date", nullable: true),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    prolongation_warning_months = table.Column<int>(type: "integer", nullable: false, defaultValue: 3),
                    prolongation_alert_months = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    prolongation_for_years = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contracts", x => x.id);
                    table.CheckConstraint("ck_contracts_renewal_settings", "prolongation_warning_months >= 0 AND prolongation_alert_months >= 0 AND prolongation_for_years > 0 AND prolongation_alert_months < prolongation_warning_months");
                    table.ForeignKey(
                        name: "fk_contracts_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "prolongations",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    contract_id = table.Column<long>(type: "bigint", nullable: false),
                    assessor = table.Column<string>(type: "text", nullable: false),
                    certification_date = table.Column<DateOnly>(type: "date", nullable: false),
                    protocol_date = table.Column<DateOnly>(type: "date", nullable: true),
                    prolongation_send = table.Column<DateOnly>(type: "date", nullable: true),
                    prolongation_returned = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_prolongations", x => x.id);
                    table.CheckConstraint("ck_prolongations_date_sequence", "(protocol_date IS NULL OR protocol_date >= certification_date) AND (prolongation_send IS NULL OR (protocol_date IS NOT NULL AND prolongation_send >= protocol_date)) AND (prolongation_returned IS NULL OR (prolongation_send IS NOT NULL AND prolongation_returned >= prolongation_send))");
                    table.ForeignKey(
                        name: "fk_prolongations_contracts_contract_id",
                        column: x => x.contract_id,
                        principalTable: "contracts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_contracts_department",
                table: "contracts",
                column: "department");

            migrationBuilder.CreateIndex(
                name: "ix_contracts_employee_id",
                table: "contracts",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_contracts_valid_to",
                table: "contracts",
                column: "valid_to");

            migrationBuilder.CreateIndex(
                name: "ux_contracts_employee_id_active",
                table: "contracts",
                column: "employee_id",
                unique: true,
                filter: "active = TRUE");

            migrationBuilder.CreateIndex(
                name: "ux_employees_normalized_personal_id",
                table: "employees",
                column: "normalized_personal_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_prolongations_contract_id_certification_date",
                table: "prolongations",
                columns: new[] { "contract_id", "certification_date" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ux_prolongations_contract_id_in_progress",
                table: "prolongations",
                column: "contract_id",
                unique: true,
                filter: "prolongation_returned IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "prolongations");

            migrationBuilder.DropTable(
                name: "contracts");

            migrationBuilder.DropTable(
                name: "employees");
        }
    }
}
