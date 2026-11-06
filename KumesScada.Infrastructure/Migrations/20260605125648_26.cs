using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KumesScada.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class _26 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KumesRaporRehber",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LokasyonId = table.Column<int>(type: "int", nullable: false),
                    RehberAdi = table.Column<string>(type: "varchar(70)", nullable: true),
                    HaftaSayisi = table.Column<int>(type: "int", nullable: false),
                    Deger = table.Column<decimal>(type: "decimal(10,3)", nullable: false),
                    Silindi = table.Column<bool>(type: "bit", nullable: true),
                    KayitZamani = table.Column<DateTime>(type: "datetime", nullable: false),
                    KaydedenUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    GuncellemeZamani = table.Column<DateTime>(type: "datetime", nullable: true),
                    GuncelleyenUserId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KumesRaporRehber", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KumesRaporRehber");
        }
    }
}
