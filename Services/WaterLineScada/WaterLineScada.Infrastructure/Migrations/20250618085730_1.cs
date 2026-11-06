using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WaterLineScada.Infrastructure.Migrations
{
    public partial class _1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ModTuru",
                table: "KullaniciSekilYetki",
                type: "varchar(20)",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "PozisyonX",
                table: "KullaniciSekilYetki",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "PozisyonY",
                table: "KullaniciSekilYetki",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Visible",
                table: "KullaniciSekilYetki",
                type: "bit",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ModTuru",
                table: "KullaniciSekilYetki");

            migrationBuilder.DropColumn(
                name: "PozisyonX",
                table: "KullaniciSekilYetki");

            migrationBuilder.DropColumn(
                name: "PozisyonY",
                table: "KullaniciSekilYetki");

            migrationBuilder.DropColumn(
                name: "Visible",
                table: "KullaniciSekilYetki");
        }
    }
}
