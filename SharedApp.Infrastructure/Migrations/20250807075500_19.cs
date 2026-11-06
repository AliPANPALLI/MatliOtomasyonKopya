using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SharedApp.Infrastructure.Migrations
{
    public partial class _19 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "KonveyorAlarmLogIds",
                table: "MailGonderimTakip",
                type: "varchar(MAX)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KumesAlarmLogIds",
                table: "MailGonderimTakip",
                type: "varchar(MAX)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SuHatAlarmLogIds",
                table: "MailGonderimTakip",
                type: "varchar(MAX)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "KonveyorAlarmLogIds",
                table: "MailGonderimTakip");

            migrationBuilder.DropColumn(
                name: "KumesAlarmLogIds",
                table: "MailGonderimTakip");

            migrationBuilder.DropColumn(
                name: "SuHatAlarmLogIds",
                table: "MailGonderimTakip");
        }
    }
}
