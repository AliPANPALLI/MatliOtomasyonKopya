using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnergyMonitoring.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class _22 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "GES",
                table: "Makine",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "GES",
                table: "Cihaz",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "GES",
                table: "Bolum",
                type: "bit",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GES",
                table: "Makine");

            migrationBuilder.DropColumn(
                name: "GES",
                table: "Cihaz");

            migrationBuilder.DropColumn(
                name: "GES",
                table: "Bolum");
        }
    }
}
