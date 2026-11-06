using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SuHatScada.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class OptimizeEtiketLogYeniGunSonuLookup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_EtiketLogYeni_EtiketId_KayitZamani_Id",
                table: "EtiketLogYeni",
                columns: new[] { "EtiketId", "KayitZamani", "Id" },
                descending: new[] { false, true, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EtiketLogYeni_EtiketId_KayitZamani_Id",
                table: "EtiketLogYeni");
        }
    }
}
