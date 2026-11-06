using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KumesScada.Infrastructure.Migrations
{
    public partial class _4 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EtiketIdForKonveyorRapor",
                table: "Konveyor",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "KonveyorRapor",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KonveyorId = table.Column<int>(type: "int", nullable: true),
                    KayitZamani = table.Column<DateTime>(type: "datetime", nullable: true),
                    Calisiyor = table.Column<bool>(type: "bit", nullable: true),
                    Ariza = table.Column<bool>(type: "bit", nullable: true),
                    Bosta = table.Column<bool>(type: "bit", nullable: true),
                    Durum = table.Column<byte>(type: "tinyint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KonveyorRapor", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KonveyorRapor");

            migrationBuilder.DropColumn(
                name: "EtiketIdForKonveyorRapor",
                table: "Konveyor");
        }
    }
}
