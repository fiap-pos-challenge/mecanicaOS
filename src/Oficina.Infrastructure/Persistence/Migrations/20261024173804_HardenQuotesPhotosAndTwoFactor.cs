using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oficina.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HardenQuotesPhotosAndTwoFactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "twoFactorSecret",
                table: "users",
                type: "varchar(512)",
                unicode: false,
                maxLength: 512,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(64)",
                oldUnicode: false,
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "photoCount",
                table: "serviceOrders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("UPDATE serviceOrders SET photoCount = (SELECT COUNT(*) FROM serviceOrderPhotos WHERE serviceOrderPhotos.serviceOrderId = serviceOrders.id)");

            migrationBuilder.AddColumn<byte[]>(
                name: "rowVersion",
                table: "estimates",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "photoCount",
                table: "serviceOrders");

            migrationBuilder.DropColumn(
                name: "rowVersion",
                table: "estimates");

            migrationBuilder.AlterColumn<string>(
                name: "twoFactorSecret",
                table: "users",
                type: "varchar(64)",
                unicode: false,
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(512)",
                oldUnicode: false,
                oldMaxLength: 512,
                oldNullable: true);
        }
    }
}
