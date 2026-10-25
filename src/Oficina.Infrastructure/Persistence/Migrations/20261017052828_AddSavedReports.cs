using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oficina.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSavedReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "savedReports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    definitionJson = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    scheduleJson = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    updatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    lastSentAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_savedReports", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ux_savedReports_name",
                table: "savedReports",
                column: "name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "savedReports");
        }
    }
}
