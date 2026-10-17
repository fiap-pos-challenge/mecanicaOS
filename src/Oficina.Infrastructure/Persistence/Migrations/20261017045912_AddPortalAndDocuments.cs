using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oficina.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPortalAndDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "issuedDocuments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    kind = table.Column<string>(type: "varchar(15)", unicode: false, maxLength: 15, nullable: false),
                    serviceOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    issuedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    snapshot = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_issuedDocuments", x => x.id);
                    table.ForeignKey(
                        name: "fk_issuedDocuments_serviceOrders_serviceOrderId",
                        column: x => x.serviceOrderId,
                        principalTable: "serviceOrders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "portalCharges",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    estimateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    serviceOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    method = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    txId = table.Column<string>(type: "varchar(25)", unicode: false, maxLength: 25, nullable: false),
                    expiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    status = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    paidAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_portalCharges", x => x.id);
                    table.ForeignKey(
                        name: "fk_portalCharges_estimates_estimateId",
                        column: x => x.estimateId,
                        principalTable: "estimates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_portalCharges_serviceOrders_serviceOrderId",
                        column: x => x.serviceOrderId,
                        principalTable: "serviceOrders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "portalSessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    tokenHash = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: false),
                    estimateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    expiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_portalSessions", x => x.id);
                    table.ForeignKey(
                        name: "fk_portalSessions_estimates_estimateId",
                        column: x => x.estimateId,
                        principalTable: "estimates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_issuedDocuments_serviceOrderId",
                table: "issuedDocuments",
                column: "serviceOrderId");

            migrationBuilder.CreateIndex(
                name: "ux_issuedDocuments_code",
                table: "issuedDocuments",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_portalCharges_estimateId",
                table: "portalCharges",
                column: "estimateId");

            migrationBuilder.CreateIndex(
                name: "ix_portalCharges_serviceOrderId",
                table: "portalCharges",
                column: "serviceOrderId");

            migrationBuilder.CreateIndex(
                name: "ux_portalCharges_txId",
                table: "portalCharges",
                column: "txId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_portalSessions_estimateId",
                table: "portalSessions",
                column: "estimateId");

            migrationBuilder.CreateIndex(
                name: "ux_portalSessions_tokenHash",
                table: "portalSessions",
                column: "tokenHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "issuedDocuments");

            migrationBuilder.DropTable(
                name: "portalCharges");

            migrationBuilder.DropTable(
                name: "portalSessions");
        }
    }
}
