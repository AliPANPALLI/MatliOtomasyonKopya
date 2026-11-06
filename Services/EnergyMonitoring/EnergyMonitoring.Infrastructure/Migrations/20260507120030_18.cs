using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnergyMonitoring.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class _18 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "Yekdem",
                table: "EnerjiMaliyetParametreler",
                type: "decimal(8,3)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(8,3)");

            migrationBuilder.AlterColumn<decimal>(
                name: "DagitimBedeli",
                table: "EnerjiMaliyetParametreler",
                type: "decimal(8,3)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(8,3)");

            migrationBuilder.AlterColumn<decimal>(
                name: "AnlasmaOrani",
                table: "EnerjiMaliyetParametreler",
                type: "decimal(8,3)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(8,3)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "Yekdem",
                table: "EnerjiMaliyetParametreler",
                type: "decimal(8,3)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(8,3)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "DagitimBedeli",
                table: "EnerjiMaliyetParametreler",
                type: "decimal(8,3)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(8,3)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "AnlasmaOrani",
                table: "EnerjiMaliyetParametreler",
                type: "decimal(8,3)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(8,3)",
                oldNullable: true);
        }
    }
}
