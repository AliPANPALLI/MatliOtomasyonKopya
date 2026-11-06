using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SharedApp.Infrastructure.Migrations
{
    public partial class _25 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TopbarNesnesi",
                table: "Modul");

            migrationBuilder.AddColumn<bool>(
                name: "SingleTopbarNesnesi",
                table: "Form",
                type: "bit",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SingleTopbarNesnesi",
                table: "Form");

            migrationBuilder.AddColumn<bool>(
                name: "TopbarNesnesi",
                table: "Modul",
                type: "bit",
                nullable: true);
        }
    }
}
