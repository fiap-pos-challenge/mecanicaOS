using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oficina.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceTermsAndPriceHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "commissionPercent",
                table: "services",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "warrantyDays",
                table: "services",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "servicePriceChanges",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    serviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    oldPrice = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    newPrice = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    changedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    changedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_servicePriceChanges", x => x.id);
                    table.ForeignKey(
                        name: "fk_servicePriceChanges_services_serviceId",
                        column: x => x.serviceId,
                        principalTable: "services",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_servicePriceChanges_service",
                table: "servicePriceChanges",
                columns: new[] { "serviceId", "changedAt" });

            migrationBuilder.Sql(
                "INSERT INTO servicePriceChanges (id, serviceId, oldPrice, newPrice, changedBy, changedAt) " +
                "SELECT NEWID(), id, NULL, basePrice, NULL, createdAt FROM services;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "servicePriceChanges");

            migrationBuilder.DropColumn(
                name: "commissionPercent",
                table: "services");

            migrationBuilder.DropColumn(
                name: "warrantyDays",
                table: "services");
        }
    }
}
