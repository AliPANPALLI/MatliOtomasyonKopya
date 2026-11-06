using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PoultryHouseScada.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SapKantarYemTuketimAlanlari : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "HesaplananYemTuketimi",
                table: "KumesLogYeni",
                type: "decimal(12,3)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SapGunlukYemGirisi",
                table: "KumesLogYeni",
                type: "decimal(12,3)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HesaplananYemTuketimi",
                table: "KumesLogYeni");

            migrationBuilder.DropColumn(
                name: "SapGunlukYemGirisi",
                table: "KumesLogYeni");
        }
    }
}
