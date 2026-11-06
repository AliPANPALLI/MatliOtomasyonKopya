using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeighbridgeTracking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class _1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Firma",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "varchar(300)", nullable: true),
                    Silindi = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Firma", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KantarKalite",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KaliteVeri1 = table.Column<string>(type: "varchar(100)", nullable: true),
                    KaliteVeri2 = table.Column<string>(type: "varchar(100)", nullable: true),
                    KayitZamani = table.Column<DateTime>(type: "datetime", nullable: true),
                    GuncelleyenUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Silindi = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KantarKalite", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KantarGuvenlik",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Plaka = table.Column<string>(type: "varchar(100)", nullable: true),
                    FirmaId = table.Column<int>(type: "int", nullable: true),
                    KayitZamani = table.Column<DateTime>(type: "datetime", nullable: true),
                    GuncelleyenUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Silindi = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KantarGuvenlik", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KantarGuvenlik_Firma_FirmaId",
                        column: x => x.FirmaId,
                        principalTable: "Firma",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KantarGuvenlik_FirmaId",
                table: "KantarGuvenlik",
                column: "FirmaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KantarGuvenlik");

            migrationBuilder.DropTable(
                name: "KantarKalite");

            migrationBuilder.DropTable(
                name: "Firma");
        }
    }
}
