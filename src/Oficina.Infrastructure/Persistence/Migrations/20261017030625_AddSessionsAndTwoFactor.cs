using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oficina.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionsAndTwoFactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "twoFactorEnabled",
                table: "users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "twoFactorSecret",
                table: "users",
                type: "varchar(64)",
                unicode: false,
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "reauthentications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    userId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    tokenHash = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: false),
                    expiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    usedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reauthentications", x => x.id);
                    table.ForeignKey(
                        name: "fk_reauthentications_users_userId",
                        column: x => x.userId,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "twoFactorChallenges",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    userId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    expiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    attempts = table.Column<int>(type: "int", nullable: false),
                    completedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_twoFactorChallenges", x => x.id);
                    table.ForeignKey(
                        name: "fk_twoFactorChallenges_users_userId",
                        column: x => x.userId,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "userSessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    userId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    tokenHash = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: false),
                    device = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    createdAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    lastSeenAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    expiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    revokedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_userSessions", x => x.id);
                    table.ForeignKey(
                        name: "fk_userSessions_users_userId",
                        column: x => x.userId,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_reauthentications_userId",
                table: "reauthentications",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "ux_reauthentications_tokenHash",
                table: "reauthentications",
                column: "tokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_twoFactorChallenges_userId",
                table: "twoFactorChallenges",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "ix_userSessions_userId",
                table: "userSessions",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "ux_userSessions_tokenHash",
                table: "userSessions",
                column: "tokenHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "reauthentications");

            migrationBuilder.DropTable(
                name: "twoFactorChallenges");

            migrationBuilder.DropTable(
                name: "userSessions");

            migrationBuilder.DropColumn(
                name: "twoFactorEnabled",
                table: "users");

            migrationBuilder.DropColumn(
                name: "twoFactorSecret",
                table: "users");
        }
    }
}
