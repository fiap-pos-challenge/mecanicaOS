using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oficina.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "negative",
                table: "stockMovements",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "serviceOrderNumber",
                table: "stockMovements",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "location",
                table: "parts",
                type: "varchar(10)",
                unicode: false,
                maxLength: 10,
                nullable: false,
                defaultValue: "GERAL");

            migrationBuilder.CreateTable(
                name: "stockCounts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    location = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    openedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    openedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    submittedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    decidedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    decidedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    rejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    lines = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    createdAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    deletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stockCounts", x => x.id);
                    table.ForeignKey(
                        name: "fk_stockCounts_users_decidedBy",
                        column: x => x.decidedBy,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stockCounts_users_openedBy",
                        column: x => x.openedBy,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "suppliers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    document = table.Column<string>(type: "varchar(14)", unicode: false, maxLength: 14, nullable: true),
                    email = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    phone = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    leadTimeDays = table.Column<int>(type: "int", nullable: false),
                    isActive = table.Column<bool>(type: "bit", nullable: false),
                    createdAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    deletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_suppliers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "purchaseOrders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    number = table.Column<long>(type: "bigint", nullable: false),
                    supplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    status = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    sentAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    lines = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    createdAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    deletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_purchaseOrders", x => x.id);
                    table.ForeignKey(
                        name: "fk_purchaseOrders_suppliers_supplierId",
                        column: x => x.supplierId,
                        principalTable: "suppliers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_purchaseOrders_supplierId",
                table: "purchaseOrders",
                column: "supplierId");

            migrationBuilder.CreateIndex(
                name: "ux_purchaseOrders_number",
                table: "purchaseOrders",
                column: "number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_stockCounts_decidedBy",
                table: "stockCounts",
                column: "decidedBy");

            migrationBuilder.CreateIndex(
                name: "ix_stockCounts_openedBy",
                table: "stockCounts",
                column: "openedBy");

            migrationBuilder.CreateIndex(
                name: "ux_suppliers_document",
                table: "suppliers",
                column: "document",
                unique: true,
                filter: "[document] IS NOT NULL AND [deletedAt] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "purchaseOrders");

            migrationBuilder.DropTable(
                name: "stockCounts");

            migrationBuilder.DropTable(
                name: "suppliers");

            migrationBuilder.DropColumn(
                name: "negative",
                table: "stockMovements");

            migrationBuilder.DropColumn(
                name: "serviceOrderNumber",
                table: "stockMovements");

            migrationBuilder.DropColumn(
                name: "location",
                table: "parts");
        }
    }
}
