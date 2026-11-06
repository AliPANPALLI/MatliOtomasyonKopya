using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PoultryHouseScada.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class _31 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "IcmeSuyuSayac",
                table: "KumesLogYeni",
                type: "decimal(18,3)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PedSuSayac",
                table: "KumesLogYeni",
                type: "decimal(18,3)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IcmeSuyuSayac",
                table: "KumesLogYeni");

            migrationBuilder.DropColumn(
                name: "PedSuSayac",
                table: "KumesLogYeni");
        }
    }
}