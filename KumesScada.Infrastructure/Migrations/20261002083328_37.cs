using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KumesScada.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class _37 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "YumurtlamaDonemleri",
                columns: table => new
                {
                    YumurtlamaDonemId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KumeslerId = table.Column<int>(type: "int", nullable: false),
                    GirisTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BaslangicHayvanSayisi = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Irk = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GuncellemeTarihi = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_YumurtlamaDonemleri", x => x.YumurtlamaDonemId);
                    table.ForeignKey(
                        name: "FK_YumurtlamaDonemleri_Kumesler_KumeslerId",
                        column: x => x.KumeslerId,
                        principalTable: "Kumesler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_YumurtlamaDonemleri_KumeslerId",
                table: "YumurtlamaDonemleri",
                column: "KumeslerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "YumurtlamaDonemleri");
        }
    }
}
