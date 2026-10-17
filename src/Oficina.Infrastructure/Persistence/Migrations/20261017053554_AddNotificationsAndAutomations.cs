using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oficina.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationsAndAutomations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "automationRules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    enabled = table.Column<bool>(type: "bit", nullable: false),
                    trigger = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    definitionJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    createdAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_automationRules", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    type = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    title = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    body = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    link = table.Column<string>(type: "varchar(120)", unicode: false, maxLength: 120, nullable: true),
                    aiSuggested = table.Column<bool>(type: "bit", nullable: false),
                    dedupeKey = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: true),
                    createdAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notifications", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "automationExecutions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ruleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    trigger = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    subjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    dedupeKey = table.Column<string>(type: "varchar(120)", unicode: false, maxLength: 120, nullable: false),
                    dueAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    createdAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    status = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    attempts = table.Column<int>(type: "int", nullable: false),
                    summary = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    error = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    finishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_automationExecutions", x => x.id);
                    table.ForeignKey(
                        name: "fk_automationExecutions_automationRules_ruleId",
                        column: x => x.ruleId,
                        principalTable: "automationRules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notificationReads",
                columns: table => new
                {
                    notificationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    userId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    readAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notificationReads", x => new { x.notificationId, x.userId });
                    table.ForeignKey(
                        name: "fk_notificationReads_notifications_notificationId",
                        column: x => x.notificationId,
                        principalTable: "notifications",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_automationExecutions_status_dueAt",
                table: "automationExecutions",
                columns: new[] { "status", "dueAt" });

            migrationBuilder.CreateIndex(
                name: "ux_automationExecutions_rule_dedupe",
                table: "automationExecutions",
                columns: new[] { "ruleId", "dedupeKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_notifications_createdAt",
                table: "notifications",
                column: "createdAt");

            migrationBuilder.CreateIndex(
                name: "ix_notifications_dedupeKey",
                table: "notifications",
                column: "dedupeKey");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "automationExecutions");

            migrationBuilder.DropTable(
                name: "notificationReads");

            migrationBuilder.DropTable(
                name: "automationRules");

            migrationBuilder.DropTable(
                name: "notifications");
        }
    }
}
