using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnergyMonitoring.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class _17 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AktifEnerjiBedeli",
                table: "EnerjiMaliyetParametreler",
                type: "decimal(8,3)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Gece",
                table: "EnerjiMaliyetParametreler",
                type: "decimal(8,3)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Genel",
                table: "EnerjiMaliyetParametreler",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "GucBedeli",
                table: "EnerjiMaliyetParametreler",
                type: "decimal(8,3)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Gunduz",
                table: "EnerjiMaliyetParametreler",
                type: "decimal(8,3)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Puant",
                table: "EnerjiMaliyetParametreler",
                type: "decimal(8,3)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TekZamanliTarimsal",
                table: "EnerjiMaliyetParametreler",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "UcZamanliTarimsal",
                table: "EnerjiMaliyetParametreler",
                type: "bit",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AktifEnerjiBedeli",
                table: "EnerjiMaliyetParametreler");

            migrationBuilder.DropColumn(
                name: "Gece",
                table: "EnerjiMaliyetParametreler");

            migrationBuilder.DropColumn(
                name: "Genel",
                table: "EnerjiMaliyetParametreler");

            migrationBuilder.DropColumn(
                name: "GucBedeli",
                table: "EnerjiMaliyetParametreler");

            migrationBuilder.DropColumn(
                name: "Gunduz",
                table: "EnerjiMaliyetParametreler");

            migrationBuilder.DropColumn(
                name: "Puant",
                table: "EnerjiMaliyetParametreler");

            migrationBuilder.DropColumn(
                name: "TekZamanliTarimsal",
                table: "EnerjiMaliyetParametreler");

            migrationBuilder.DropColumn(
                name: "UcZamanliTarimsal",
                table: "EnerjiMaliyetParametreler");
        }
    }
}
