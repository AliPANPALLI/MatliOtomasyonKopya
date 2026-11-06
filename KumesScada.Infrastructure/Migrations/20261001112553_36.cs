using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KumesScada.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class _36 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RaporVeri_RaporKategoriId",
                table: "RaporVeri");

            migrationBuilder.AddColumn<int>(
                name: "HaftaSayisi",
                table: "RaporVeri",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_RaporVeri_RaporKategoriId_HaftaSayisi",
                table: "RaporVeri",
                columns: new[] { "RaporKategoriId", "HaftaSayisi" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RaporVeri_RaporKategoriId_HaftaSayisi",
                table: "RaporVeri");

            migrationBuilder.DropColumn(
                name: "HaftaSayisi",
                table: "RaporVeri");

            migrationBuilder.CreateIndex(
                name: "IX_RaporVeri_RaporKategoriId",
                table: "RaporVeri",
                column: "RaporKategoriId");
        }
    }
}
