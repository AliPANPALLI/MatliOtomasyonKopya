using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SharedApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class _46 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AlarmLogId",
                table: "YanginAlgilamaLogs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AnlikFotografBoyutu",
                table: "YanginAlgilamaLogs",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AnlikFotografContentType",
                table: "YanginAlgilamaLogs",
                type: "varchar(100)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AnlikFotografDosyaAdi",
                table: "YanginAlgilamaLogs",
                type: "varchar(255)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AnlikFotografDosyaYolu",
                table: "YanginAlgilamaLogs",
                type: "varchar(500)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_YanginAlgilamaLogs_AlarmLogId_KayitZamani",
                table: "YanginAlgilamaLogs",
                columns: new[] { "AlarmLogId", "KayitZamani" });

            migrationBuilder.AddForeignKey(
                name: "FK_YanginAlgilamaLogs_AlarmLog_AlarmLogId",
                table: "YanginAlgilamaLogs",
                column: "AlarmLogId",
                principalTable: "AlarmLog",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_YanginAlgilamaLogs_AlarmLog_AlarmLogId",
                table: "YanginAlgilamaLogs");

            migrationBuilder.DropIndex(
                name: "IX_YanginAlgilamaLogs_AlarmLogId_KayitZamani",
                table: "YanginAlgilamaLogs");

            migrationBuilder.DropColumn(
                name: "AlarmLogId",
                table: "YanginAlgilamaLogs");

            migrationBuilder.DropColumn(
                name: "AnlikFotografBoyutu",
                table: "YanginAlgilamaLogs");

            migrationBuilder.DropColumn(
                name: "AnlikFotografContentType",
                table: "YanginAlgilamaLogs");

            migrationBuilder.DropColumn(
                name: "AnlikFotografDosyaAdi",
                table: "YanginAlgilamaLogs");

            migrationBuilder.DropColumn(
                name: "AnlikFotografDosyaYolu",
                table: "YanginAlgilamaLogs");
        }
    }
}
