using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SharedApp.Infrastructure.Migrations
{
    public partial class _7 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AlarmGrupId",
                table: "Alarmlar");

            migrationBuilder.CreateTable(
                name: "AlarmGrupAlarmEsleme",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AlarmGrupId = table.Column<int>(type: "int", nullable: true),
                    AlarmId = table.Column<int>(type: "int", nullable: true),
                    Silindi = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlarmGrupAlarmEsleme", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlarmGrupAlarmEsleme");

            migrationBuilder.AddColumn<int>(
                name: "AlarmGrupId",
                table: "Alarmlar",
                type: "int",
                nullable: true);
        }
    }
}
