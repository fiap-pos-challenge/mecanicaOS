using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oficina.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerConsentAndPrivacyAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "consentEmail",
                table: "customers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "consentUpdatedAt",
                table: "customers",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "consentWhatsApp",
                table: "customers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "sensitiveDataAccess",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    userId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    customerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    field = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    reason = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    traceId = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: true),
                    occurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sensitiveDataAccess", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_sensitiveDataAccess_customer",
                table: "sensitiveDataAccess",
                columns: new[] { "customerId", "occurredAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sensitiveDataAccess");

            migrationBuilder.DropColumn(
                name: "consentEmail",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "consentUpdatedAt",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "consentWhatsApp",
                table: "customers");
        }
    }
}
