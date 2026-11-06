using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KumesScada.Infrastructure.Migrations
{
    public partial class _21 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KumesYemRasyonEsleme",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KumesId = table.Column<int>(type: "int", nullable: false),
                    YemRasyonId = table.Column<int>(type: "int", nullable: false),
                    KayitZamani = table.Column<DateTime>(type: "datetime", nullable: true),
                    GecerlilikBaslangicZamani = table.Column<DateTime>(type: "datetime", nullable: true),
                    GecerlilikBitisZamani = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KumesYemRasyonEsleme", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "YemRasyon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    YemAdi = table.Column<string>(type: "varchar(70)", nullable: true),
                    Rasyon = table.Column<string>(type: "varchar(70)", nullable: true),
                    BirimFiyat = table.Column<decimal>(type: "decimal(15,3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_YemRasyon", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KumesYemRasyonEsleme");

            migrationBuilder.DropTable(
                name: "YemRasyon");
        }
    }
}
