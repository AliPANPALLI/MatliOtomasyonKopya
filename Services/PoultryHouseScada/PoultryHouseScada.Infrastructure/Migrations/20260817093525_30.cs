using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PoultryHouseScada.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class _30 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PedSuTuketimBugun",
                table: "KumesLogYeni",
                type: "decimal(8,3)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PedSuTuketimBugun",
                table: "KumesLog_TempYeni",
                type: "decimal(8,3)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EslemeSozcugu",
                table: "Etiket",
                type: "varchar(100)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PedSuTuketimBugun",
                table: "KumesLogYeni");

            migrationBuilder.DropColumn(
                name: "PedSuTuketimBugun",
                table: "KumesLog_TempYeni");

            migrationBuilder.DropColumn(
                name: "EslemeSozcugu",
                table: "Etiket");
        }
    }
}
