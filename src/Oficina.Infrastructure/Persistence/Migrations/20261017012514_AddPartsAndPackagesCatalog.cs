using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oficina.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPartsAndPackagesCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "barcode",
                table: "parts",
                type: "varchar(13)",
                unicode: false,
                maxLength: 13,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "cost",
                table: "parts",
                type: "decimal(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "supplier",
                table: "parts",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "packages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    price = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    minMileage = table.Column<int>(type: "int", nullable: true),
                    maxMileage = table.Column<int>(type: "int", nullable: true),
                    isActive = table.Column<bool>(type: "bit", nullable: false),
                    createdAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    deletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_packages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "priceChanges",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    kind = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    itemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    field = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    oldValue = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    newValue = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    changedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    changedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_priceChanges", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "serviceUsualParts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    partId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    quantity = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: false),
                    serviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_serviceUsualParts", x => x.id);
                    table.ForeignKey(
                        name: "fk_serviceUsualParts_parts_partId",
                        column: x => x.partId,
                        principalTable: "parts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_serviceUsualParts_services_serviceId",
                        column: x => x.serviceId,
                        principalTable: "services",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "packageItems",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    kind = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    itemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    quantity = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: false),
                    packageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_packageItems", x => x.id);
                    table.ForeignKey(
                        name: "fk_packageItems_packages_packageId",
                        column: x => x.packageId,
                        principalTable: "packages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_packageItems_item",
                table: "packageItems",
                columns: new[] { "kind", "itemId" });

            migrationBuilder.CreateIndex(
                name: "ix_packageItems_packageId",
                table: "packageItems",
                column: "packageId");

            migrationBuilder.CreateIndex(
                name: "ix_priceChanges_item",
                table: "priceChanges",
                columns: new[] { "kind", "itemId", "changedAt" });

            migrationBuilder.CreateIndex(
                name: "ix_serviceUsualParts_partId",
                table: "serviceUsualParts",
                column: "partId");

            migrationBuilder.CreateIndex(
                name: "ix_serviceUsualParts_serviceId",
                table: "serviceUsualParts",
                column: "serviceId");

            migrationBuilder.Sql(
                "INSERT INTO priceChanges (id, kind, itemId, field, oldValue, newValue, changedBy, changedAt) " +
                "SELECT id, 'Service', serviceId, 'BasePrice', oldPrice, newPrice, changedBy, changedAt FROM servicePriceChanges;");

            migrationBuilder.Sql(
                "INSERT INTO priceChanges (id, kind, itemId, field, oldValue, newValue, changedBy, changedAt) " +
                "SELECT NEWID(), 'Part', id, 'UnitPrice', NULL, unitPrice, NULL, createdAt FROM parts;");

            migrationBuilder.DropTable(
                name: "servicePriceChanges");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "packageItems");

            migrationBuilder.DropTable(
                name: "priceChanges");

            migrationBuilder.DropTable(
                name: "serviceUsualParts");

            migrationBuilder.DropTable(
                name: "packages");

            migrationBuilder.DropColumn(
                name: "barcode",
                table: "parts");

            migrationBuilder.DropColumn(
                name: "cost",
                table: "parts");

            migrationBuilder.DropColumn(
                name: "supplier",
                table: "parts");

            migrationBuilder.CreateTable(
                name: "servicePriceChanges",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    changedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    changedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    newPrice = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    oldPrice = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    serviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
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
        }
    }
}
