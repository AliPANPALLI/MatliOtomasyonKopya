using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PoultryHouseScada.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class _27 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KumesHaftalikCatiKontrol",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KumesId = table.Column<int>(type: "int", nullable: false),
                    DonemId = table.Column<int>(type: "int", nullable: true),
                    HaftaSayisi = table.Column<int>(type: "int", nullable: true),
                    Deger = table.Column<decimal>(type: "decimal(10,3)", nullable: true),
                    KayitZamani = table.Column<DateTime>(type: "datetime", nullable: true),
                    KayitYapanUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    GuncellemeZamani = table.Column<DateTime>(type: "datetime", nullable: true),
                    GuncelleyenUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Silindi = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KumesHaftalikCatiKontrol", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KumesHaftalikCatiKontrol");
        }
    }
}
