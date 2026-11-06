using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KumesScada.Infrastructure.Migrations
{
    public partial class _14 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "YTKNodeRed",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KayitZamaniNodeRed = table.Column<DateTime>(type: "datetime", nullable: true),
                    KayitZamani = table.Column<DateTime>(type: "datetime", nullable: true),
                    Veriler = table.Column<string>(type: "varchar(MAX)", nullable: false),
                    LokasyonId = table.Column<int>(type: "int", nullable: false),
                    Durum = table.Column<int>(type: "int", nullable: false),
                    PlcAdi = table.Column<string>(type: "varchar(50)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_YTKNodeRed", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "YTKNodeRed");
        }
    }
}
