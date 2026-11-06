using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SharedApp.Infrastructure.Migrations
{
    public partial class _8 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "KonveyorId",
                table: "Alarmlar",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "KumesId",
                table: "Alarmlar",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LokasyonId",
                table: "Alarmlar",
                type: "int",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "KonveyorId",
                table: "Alarmlar");

            migrationBuilder.DropColumn(
                name: "KumesId",
                table: "Alarmlar");

            migrationBuilder.DropColumn(
                name: "LokasyonId",
                table: "Alarmlar");
        }
    }
}
