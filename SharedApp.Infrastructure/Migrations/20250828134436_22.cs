using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SharedApp.Infrastructure.Migrations
{
    public partial class _22 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MesajGonderimTakip",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KayitZamani = table.Column<DateTime>(type: "datetime", nullable: true),
                    SmsIcerik = table.Column<string>(type: "varchar(MAX)", nullable: true),
                    GonderimBasarili = table.Column<bool>(type: "bit", nullable: true),
                    HataMesaji = table.Column<string>(type: "varchar(MAX)", nullable: true),
                    TelefonNo = table.Column<string>(type: "varchar(50)", nullable: true),
                    SonDenemeZamani = table.Column<DateTime>(type: "datetime", nullable: true),
                    KalanDenemeSayisi = table.Column<short>(type: "smallint", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    KumesAlarmLogIds = table.Column<string>(type: "varchar(MAX)", nullable: true),
                    KonveyorAlarmLogIds = table.Column<string>(type: "varchar(MAX)", nullable: true),
                    SuHatAlarmLogIds = table.Column<string>(type: "varchar(MAX)", nullable: true),
                    SeviyeNo = table.Column<int>(type: "int", nullable: true),
                    SesDosyaYolu = table.Column<string>(type: "varchar(500)", nullable: true),
                    AramaSure = table.Column<int>(type: "int", nullable: true),
                    CevaplandiMi = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MesajGonderimTakip", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MesajGonderimTakip");
        }
    }
}
