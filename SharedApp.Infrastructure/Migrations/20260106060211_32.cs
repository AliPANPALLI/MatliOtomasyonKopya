using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SharedApp.Infrastructure.Migrations
{
    public partial class _32 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "MesajGonderimTakipId",
                table: "MesajGonderimTakipAlarmLog",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "TakipId",
                table: "MesajGonderimTakipAlarmLog",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Tesis",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "varchar(50)", nullable: false),
                    LokasyonId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tesis", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tesis_Lokasyon_LokasyonId",
                        column: x => x.LokasyonId,
                        principalTable: "Lokasyon",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Bolum",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "varchar(50)", nullable: false),
                    TesisId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bolum", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bolum_Tesis_TesisId",
                        column: x => x.TesisId,
                        principalTable: "Tesis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MesajGonderimTakipAlarmLog_AlarmKaynakTuru_AlarmLogId",
                table: "MesajGonderimTakipAlarmLog",
                columns: new[] { "AlarmKaynakTuru", "AlarmLogId" });

            migrationBuilder.CreateIndex(
                name: "IX_MesajGonderimTakipAlarmLog_MesajGonderimTakipId_AlarmLogId_AlarmKaynakTuru_TakipTipi",
                table: "MesajGonderimTakipAlarmLog",
                columns: new[] { "MesajGonderimTakipId", "AlarmLogId", "AlarmKaynakTuru", "TakipTipi" },
                unique: true,
                filter: "[MesajGonderimTakipId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Bolum_TesisId",
                table: "Bolum",
                column: "TesisId");

            migrationBuilder.CreateIndex(
                name: "IX_Tesis_LokasyonId",
                table: "Tesis",
                column: "LokasyonId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Bolum");

            migrationBuilder.DropTable(
                name: "Tesis");

            migrationBuilder.DropIndex(
                name: "IX_MesajGonderimTakipAlarmLog_AlarmKaynakTuru_AlarmLogId",
                table: "MesajGonderimTakipAlarmLog");

            migrationBuilder.DropIndex(
                name: "IX_MesajGonderimTakipAlarmLog_MesajGonderimTakipId_AlarmLogId_AlarmKaynakTuru_TakipTipi",
                table: "MesajGonderimTakipAlarmLog");

            migrationBuilder.DropColumn(
                name: "TakipId",
                table: "MesajGonderimTakipAlarmLog");

            migrationBuilder.AlterColumn<int>(
                name: "MesajGonderimTakipId",
                table: "MesajGonderimTakipAlarmLog",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
