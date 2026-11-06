using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SuHatScada.Infrastructure.Migrations
{
    public partial class _12 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SuIzlemeNodeRed",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KayitZamaniNodeRed = table.Column<DateTime>(type: "datetime", nullable: true),
                    KayitZamani = table.Column<DateTime>(type: "datetime", nullable: true),
                    Veriler = table.Column<string>(type: "varchar(MAX)", nullable: false),
                    LokasyonId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuIzlemeNodeRed", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SuIzlemeNodeRed");
        }
    }
}
