using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PoultryHouseScada.Infrastructure.Migrations
{
    public partial class _22 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "GecerlilikBaslangicZamani",
                table: "YemRasyon",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "GecerlilikBitisZamani",
                table: "YemRasyon",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LokasyonId",
                table: "KumesYemRasyonEsleme",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GecerlilikBaslangicZamani",
                table: "YemRasyon");

            migrationBuilder.DropColumn(
                name: "GecerlilikBitisZamani",
                table: "YemRasyon");

            migrationBuilder.DropColumn(
                name: "LokasyonId",
                table: "KumesYemRasyonEsleme");
        }
    }
}
