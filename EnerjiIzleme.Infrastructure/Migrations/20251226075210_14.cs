using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnerjiIzleme.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class _14 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Aktif",
                table: "Makine",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Aktif",
                table: "Bolum",
                type: "bit",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Aktif",
                table: "Makine");

            migrationBuilder.DropColumn(
                name: "Aktif",
                table: "Bolum");
        }
    }
}
