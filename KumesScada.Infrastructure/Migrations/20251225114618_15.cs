using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KumesScada.Infrastructure.Migrations
{
    public partial class _15 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EtiketLog_TempYeni",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EtiketId = table.Column<int>(type: "int", nullable: true),
                    KumesId = table.Column<int>(type: "int", nullable: true),
                    KonveyorId = table.Column<int>(type: "int", nullable: true),
                    KayitZamani = table.Column<DateTime>(type: "datetime", nullable: true),
                    Deger = table.Column<decimal>(type: "decimal(18,3)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EtiketLog_TempYeni", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KumesLog_TempYeni",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KumesId = table.Column<int>(type: "int", nullable: true),
                    KayitZamani = table.Column<DateTime>(type: "datetime", nullable: true),
                    DonemId = table.Column<int>(type: "int", nullable: true),
                    SicaklikKumes = table.Column<decimal>(type: "decimal(8,3)", nullable: true),
                    SicaklikDis = table.Column<decimal>(type: "decimal(8,3)", nullable: true),
                    Sicaklik1 = table.Column<decimal>(type: "decimal(8,3)", nullable: true),
                    Sicaklik2 = table.Column<decimal>(type: "decimal(8,3)", nullable: true),
                    Sicaklik3 = table.Column<decimal>(type: "decimal(8,3)", nullable: true),
                    Sicaklik4 = table.Column<decimal>(type: "decimal(8,3)", nullable: true),
                    Sicaklik5 = table.Column<decimal>(type: "decimal(8,3)", nullable: true),
                    Sicaklik6 = table.Column<decimal>(type: "decimal(8,3)", nullable: true),
                    Sicaklik7 = table.Column<decimal>(type: "decimal(8,3)", nullable: true),
                    Sicaklik8 = table.Column<decimal>(type: "decimal(8,3)", nullable: true),
                    NemKumes = table.Column<decimal>(type: "decimal(8,3)", nullable: true),
                    NemDis = table.Column<decimal>(type: "decimal(8,3)", nullable: true),
                    Nem1 = table.Column<decimal>(type: "decimal(8,3)", nullable: true),
                    Nem2 = table.Column<decimal>(type: "decimal(8,3)", nullable: true),
                    Nem3 = table.Column<decimal>(type: "decimal(8,3)", nullable: true),
                    Nem4 = table.Column<decimal>(type: "decimal(8,3)", nullable: true),
                    Nem5 = table.Column<decimal>(type: "decimal(8,3)", nullable: true),
                    Nem6 = table.Column<decimal>(type: "decimal(8,3)", nullable: true),
                    Nem7 = table.Column<decimal>(type: "decimal(8,3)", nullable: true),
                    Nem8 = table.Column<decimal>(type: "decimal(8,3)", nullable: true),
                    Silo1 = table.Column<decimal>(type: "decimal(8,3)", nullable: true),
                    Silo2 = table.Column<decimal>(type: "decimal(8,3)", nullable: true),
                    Basinc1 = table.Column<decimal>(type: "decimal(8,3)", nullable: true),
                    Co2_1 = table.Column<decimal>(type: "decimal(8,3)", nullable: true),
                    SuTuketimBugun = table.Column<decimal>(type: "decimal(8,3)", nullable: true),
                    YemTuketimBugun = table.Column<decimal>(type: "decimal(8,3)", nullable: true),
                    OlenSayisiBugun = table.Column<int>(type: "int", nullable: true),
                    YumurtaAdet = table.Column<int>(type: "int", nullable: true),
                    Silindi = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KumesLog_TempYeni", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EtiketLog_TempYeni");

            migrationBuilder.DropTable(
                name: "KumesLog_TempYeni");
        }
    }
}
