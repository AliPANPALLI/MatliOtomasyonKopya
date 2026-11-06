using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnergyMonitoring.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class _20 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EnerjiMaliyetGrupId",
                table: "EnerjiMaliyetParametreler",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EnerjiMaliyetGrup",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "varchar(250)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnerjiMaliyetGrup", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EnerjiMaliyetParametreAtama",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EnerjiMaliyetGrupId = table.Column<int>(type: "int", nullable: false),
                    NodeId = table.Column<int>(type: "int", nullable: false),
                    NodeType = table.Column<string>(type: "varchar(50)", nullable: true),
                    KayitZamani = table.Column<DateTime>(type: "datetime", nullable: true),
                    Aktif = table.Column<bool>(type: "bit", nullable: true),
                    Silindi = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnerjiMaliyetParametreAtama", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EnerjiMaliyetGrup");

            migrationBuilder.DropTable(
                name: "EnerjiMaliyetParametreAtama");

            migrationBuilder.DropColumn(
                name: "EnerjiMaliyetGrupId",
                table: "EnerjiMaliyetParametreler");
        }
    }
}
