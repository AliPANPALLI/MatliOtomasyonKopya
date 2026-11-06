using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SharedApp.Infrastructure.Migrations
{
    public partial class _11 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "KonveyorAlarmLogId",
                table: "MailGonderimTakip",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "KumesAlarmLogId",
                table: "MailGonderimTakip",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SuHatAlarmLogId",
                table: "MailGonderimTakip",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "MailGonderimTakip",
                type: "nvarchar(450)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "KonveyorAlarmLogId",
                table: "MailGonderimTakip");

            migrationBuilder.DropColumn(
                name: "KumesAlarmLogId",
                table: "MailGonderimTakip");

            migrationBuilder.DropColumn(
                name: "SuHatAlarmLogId",
                table: "MailGonderimTakip");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "MailGonderimTakip");
        }
    }
}
