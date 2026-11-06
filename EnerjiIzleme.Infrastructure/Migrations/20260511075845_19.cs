using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnerjiIzleme.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class _19 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "Yekdem",
                table: "EnerjiMaliyetParametreler",
                type: "decimal(18,6)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(8,3)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Puant",
                table: "EnerjiMaliyetParametreler",
                type: "decimal(18,6)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(8,3)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Gunduz",
                table: "EnerjiMaliyetParametreler",
                type: "decimal(18,6)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(8,3)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "GucBedeli",
                table: "EnerjiMaliyetParametreler",
                type: "decimal(18,6)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(8,3)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Gece",
                table: "EnerjiMaliyetParametreler",
                type: "decimal(18,6)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(8,3)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "DagitimBedeli",
                table: "EnerjiMaliyetParametreler",
                type: "decimal(18,6)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(8,3)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "AnlasmaOrani",
                table: "EnerjiMaliyetParametreler",
                type: "decimal(18,6)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(8,3)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "AktifEnerjiBedeli",
                table: "EnerjiMaliyetParametreler",
                type: "decimal(18,6)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(8,3)",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "Yekdem",
                table: "EnerjiMaliyetParametreler",
                type: "decimal(8,3)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,6)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Puant",
                table: "EnerjiMaliyetParametreler",
                type: "decimal(8,3)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,6)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Gunduz",
                table: "EnerjiMaliyetParametreler",
                type: "decimal(8,3)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,6)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "GucBedeli",
                table: "EnerjiMaliyetParametreler",
                type: "decimal(8,3)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,6)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Gece",
                table: "EnerjiMaliyetParametreler",
                type: "decimal(8,3)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,6)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "DagitimBedeli",
                table: "EnerjiMaliyetParametreler",
                type: "decimal(8,3)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,6)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "AnlasmaOrani",
                table: "EnerjiMaliyetParametreler",
                type: "decimal(8,3)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,6)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "AktifEnerjiBedeli",
                table: "EnerjiMaliyetParametreler",
                type: "decimal(8,3)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,6)",
                oldNullable: true);
        }
    }
}
