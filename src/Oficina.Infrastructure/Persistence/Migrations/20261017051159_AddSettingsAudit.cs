using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oficina.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSettingsAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "auditEntries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    occurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    actorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    actorName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    area = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    action = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    target = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    detail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auditEntries", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_auditEntries_occurredAt",
                table: "auditEntries",
                column: "occurredAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "auditEntries");
        }
    }
}
