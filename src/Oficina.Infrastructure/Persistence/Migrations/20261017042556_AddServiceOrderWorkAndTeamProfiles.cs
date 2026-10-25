using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oficina.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceOrderWorkAndTeamProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "checklist",
                table: "serviceOrders",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "diagnosis",
                table: "serviceOrders",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "mechanicId",
                table: "serviceOrders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "mileage",
                table: "serviceOrders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "promisedAt",
                table: "serviceOrders",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "reopened",
                table: "serviceOrders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "sentToQuote",
                table: "serviceOrders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "version",
                table: "serviceOrders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "serviceOrderPhotos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    serviceOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    clientId = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: true),
                    contentType = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    dataUrl = table.Column<string>(type: "varchar(max)", unicode: false, nullable: false),
                    caption = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    takenAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    createdAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    deletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_serviceOrderPhotos", x => x.id);
                    table.ForeignKey(
                        name: "fk_serviceOrderPhotos_serviceOrders_serviceOrderId",
                        column: x => x.serviceOrderId,
                        principalTable: "serviceOrders",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "teamProfiles",
                columns: table => new
                {
                    userId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    specialties = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    commissionPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    discountLimitPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    schedule = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    updatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_teamProfiles", x => x.userId);
                    table.ForeignKey(
                        name: "fk_teamProfiles_users_userId",
                        column: x => x.userId,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "timeEntries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    serviceOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    mechanicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    startedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    endedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    minutes = table.Column<int>(type: "int", nullable: false),
                    createdAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    deletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_timeEntries", x => x.id);
                    table.ForeignKey(
                        name: "fk_timeEntries_serviceOrders_serviceOrderId",
                        column: x => x.serviceOrderId,
                        principalTable: "serviceOrders",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_timeEntries_users_mechanicId",
                        column: x => x.mechanicId,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_serviceOrders_mechanicId",
                table: "serviceOrders",
                column: "mechanicId");

            migrationBuilder.CreateIndex(
                name: "ix_serviceOrderPhotos_serviceOrderId",
                table: "serviceOrderPhotos",
                column: "serviceOrderId");

            migrationBuilder.CreateIndex(
                name: "ix_timeEntries_mechanic",
                table: "timeEntries",
                columns: new[] { "mechanicId", "startedAt" });

            migrationBuilder.CreateIndex(
                name: "ix_timeEntries_serviceOrderId",
                table: "timeEntries",
                column: "serviceOrderId");

            migrationBuilder.AddForeignKey(
                name: "fk_serviceOrders_users_mechanicId",
                table: "serviceOrders",
                column: "mechanicId",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(
                @"UPDATE serviceOrders SET checklist = N'[{""id"":""item-1"",""label"":""Óleo e filtros"",""state"":null},{""id"":""item-2"",""label"":""Freios"",""state"":null},{""id"":""item-3"",""label"":""Suspensão dianteira"",""state"":null},{""id"":""item-4"",""label"":""Pneus"",""state"":null},{""id"":""item-5"",""label"":""Luzes"",""state"":null},{""id"":""item-6"",""label"":""Bateria"",""state"":null}]' WHERE checklist = N'';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_serviceOrders_users_mechanicId",
                table: "serviceOrders");

            migrationBuilder.DropTable(
                name: "serviceOrderPhotos");

            migrationBuilder.DropTable(
                name: "teamProfiles");

            migrationBuilder.DropTable(
                name: "timeEntries");

            migrationBuilder.DropIndex(
                name: "ix_serviceOrders_mechanicId",
                table: "serviceOrders");

            migrationBuilder.DropColumn(
                name: "checklist",
                table: "serviceOrders");

            migrationBuilder.DropColumn(
                name: "diagnosis",
                table: "serviceOrders");

            migrationBuilder.DropColumn(
                name: "mechanicId",
                table: "serviceOrders");

            migrationBuilder.DropColumn(
                name: "mileage",
                table: "serviceOrders");

            migrationBuilder.DropColumn(
                name: "promisedAt",
                table: "serviceOrders");

            migrationBuilder.DropColumn(
                name: "reopened",
                table: "serviceOrders");

            migrationBuilder.DropColumn(
                name: "sentToQuote",
                table: "serviceOrders");

            migrationBuilder.DropColumn(
                name: "version",
                table: "serviceOrders");
        }
    }
}
