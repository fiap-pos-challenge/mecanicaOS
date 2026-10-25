using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oficina.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleMileage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "mileage",
                table: "vehicles",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "mileage",
                table: "vehicles");
        }
    }
}
