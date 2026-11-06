using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KumesScada.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class _33 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NekropsiRaporu",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KumesId = table.Column<int>(type: "int", nullable: false),
                    Tarih = table.Column<DateTime>(type: "datetime", nullable: false),
                    Hafta = table.Column<int>(type: "int", nullable: true),
                    NekropsiSebebi = table.Column<string>(type: "varchar(500)", nullable: false),
                    Pcr = table.Column<string>(type: "varchar(20)", nullable: false, defaultValue: "BELIRTILMEDI"),
                    KayitZamani = table.Column<DateTime>(type: "datetime", nullable: false),
                    KayitYapanUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Silindi = table.Column<bool>(type: "bit", nullable: true, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NekropsiRaporu", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NekropsiBulgu",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NekropsiRaporuId = table.Column<int>(type: "int", nullable: false),
                    Key = table.Column<string>(type: "varchar(100)", nullable: false),
                    Bulunan = table.Column<int>(type: "int", nullable: true),
                    Toplam = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NekropsiBulgu", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NekropsiBulgu_NekropsiRaporu_NekropsiRaporuId",
                        column: x => x.NekropsiRaporuId,
                        principalTable: "NekropsiRaporu",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NekropsiFotograf",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NekropsiRaporuId = table.Column<int>(type: "int", nullable: false),
                    DosyaAdi = table.Column<string>(type: "varchar(260)", nullable: false),
                    GoreliDosyaYolu = table.Column<string>(type: "varchar(400)", nullable: false),
                    ContentType = table.Column<string>(type: "varchar(100)", nullable: false),
                    BoyutByte = table.Column<long>(type: "bigint", nullable: false),
                    KayitZamani = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NekropsiFotograf", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NekropsiFotograf_NekropsiRaporu_NekropsiRaporuId",
                        column: x => x.NekropsiRaporuId,
                        principalTable: "NekropsiRaporu",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NekropsiBulgu_NekropsiRaporuId",
                table: "NekropsiBulgu",
                column: "NekropsiRaporuId");

            migrationBuilder.CreateIndex(
                name: "IX_NekropsiFotograf_NekropsiRaporuId",
                table: "NekropsiFotograf",
                column: "NekropsiRaporuId");

            migrationBuilder.CreateIndex(
                name: "IX_NekropsiRaporu_KumesId",
                table: "NekropsiRaporu",
                column: "KumesId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NekropsiBulgu");

            migrationBuilder.DropTable(
                name: "NekropsiFotograf");

            migrationBuilder.DropTable(
                name: "NekropsiRaporu");
        }
    }
}
