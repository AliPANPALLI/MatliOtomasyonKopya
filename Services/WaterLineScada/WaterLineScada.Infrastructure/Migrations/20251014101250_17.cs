using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WaterLineScada.Infrastructure.Migrations
{
    public partial class _17 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SayacGunSonu",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SayacAdi = table.Column<string>(type: "varchar(50)", nullable: false),
                    KayitZamani = table.Column<DateTime>(type: "datetime", nullable: true),
                    SayacDegeri = table.Column<decimal>(type: "decimal(18,3)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SayacGunSonu", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SayacGunSonu");
        }
    }
}
