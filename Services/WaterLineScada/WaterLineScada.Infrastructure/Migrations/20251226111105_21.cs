using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WaterLineScada.Infrastructure.Migrations
{
    public partial class _21 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EtiketLog_Temp_Test");

            migrationBuilder.CreateTable(
                name: "EtiketLogYeni",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EtiketId = table.Column<int>(type: "int", nullable: true),
                    SuKaynakId = table.Column<int>(type: "int", nullable: true),
                    SayacId = table.Column<int>(type: "int", nullable: true),
                    LokasyonId = table.Column<int>(type: "int", nullable: true),
                    KayitZamani = table.Column<DateTime>(type: "datetime", nullable: true),
                    Deger = table.Column<decimal>(type: "decimal(18,3)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EtiketLogYeni", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SuKaynakLogYeni",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SuKaynakId = table.Column<int>(type: "int", nullable: true),
                    KayitZamani = table.Column<DateTime>(type: "datetime", nullable: false),
                    Basinc = table.Column<decimal>(type: "decimal(18,3)", nullable: true),
                    SeviyeMetre = table.Column<decimal>(type: "decimal(18,3)", nullable: true),
                    SeviyeYuzde = table.Column<decimal>(type: "decimal(18,3)", nullable: true),
                    SuHacim = table.Column<decimal>(type: "decimal(18,3)", nullable: true),
                    SuHacimYuzde = table.Column<decimal>(type: "decimal(18,3)", nullable: true),
                    TankHacim = table.Column<decimal>(type: "decimal(18,3)", nullable: true),
                    SayacId = table.Column<int>(type: "int", nullable: true),
                    Debi = table.Column<decimal>(type: "decimal(18,3)", nullable: true),
                    DebiSayac = table.Column<decimal>(type: "decimal(18,3)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuKaynakLogYeni", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EtiketLogYeni");

            migrationBuilder.DropTable(
                name: "SuKaynakLogYeni");

            migrationBuilder.CreateTable(
                name: "EtiketLog_Temp_Test",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Deger = table.Column<decimal>(type: "decimal(18,3)", nullable: true),
                    Durum = table.Column<byte>(type: "tinyint", nullable: true),
                    EtiketId = table.Column<int>(type: "int", nullable: true),
                    KayitZamani = table.Column<DateTime>(type: "datetime", nullable: true),
                    LokasyonId = table.Column<int>(type: "int", nullable: true),
                    SayacId = table.Column<int>(type: "int", nullable: true),
                    SuKaynakId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EtiketLog_Temp_Test", x => x.Id);
                });
        }
    }
}
