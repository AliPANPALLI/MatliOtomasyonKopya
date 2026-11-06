using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SharedApp.Infrastructure.Migrations
{
    public partial class _4 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Silindi",
                table: "Alarmlar",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Silindi",
                table: "AlarmGruplar",
                type: "bit",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Silindi",
                table: "Alarmlar");

            migrationBuilder.DropColumn(
                name: "Silindi",
                table: "AlarmGruplar");
        }
    }
}
