using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SuHatScada.Infrastructure.Migrations
{
    public partial class _6 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RadioButonModTuru",
                table: "KullaniciSekilYetki",
                type: "varchar(20)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RadioButonModTuru",
                table: "KullaniciSekilYetki");
        }
    }
}
