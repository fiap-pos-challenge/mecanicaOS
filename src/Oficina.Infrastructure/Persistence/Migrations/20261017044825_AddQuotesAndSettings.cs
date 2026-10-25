using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oficina.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddQuotesAndSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "estimates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    number = table.Column<long>(type: "bigint", nullable: false),
                    version = table.Column<int>(type: "int", nullable: false),
                    serviceOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    status = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    lines = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    discountMode = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    manualDiscount = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    paymentMethods = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    maxInstallments = table.Column<int>(type: "int", nullable: false),
                    approval = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    delivery = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    deliveryTokenHash = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: true),
                    portalFailures = table.Column<int>(type: "int", nullable: false),
                    rejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    acceptedLineIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    createdAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    deletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_estimates", x => x.id);
                    table.ForeignKey(
                        name: "fk_estimates_serviceOrders_serviceOrderId",
                        column: x => x.serviceOrderId,
                        principalTable: "serviceOrders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "settingsDocuments",
                columns: table => new
                {
                    key = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false),
                    json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    updatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_settingsDocuments", x => x.key);
                });

            migrationBuilder.CreateIndex(
                name: "ix_estimates_deliveryTokenHash",
                table: "estimates",
                column: "deliveryTokenHash");

            migrationBuilder.CreateIndex(
                name: "ix_estimates_serviceOrderId",
                table: "estimates",
                column: "serviceOrderId");

            migrationBuilder.CreateIndex(
                name: "ux_estimates_number_version",
                table: "estimates",
                columns: new[] { "number", "version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "estimates");

            migrationBuilder.DropTable(
                name: "settingsDocuments");
        }
    }
}
