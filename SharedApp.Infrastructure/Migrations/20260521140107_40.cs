using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SharedApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class _40 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ServisKontrol",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServisName = table.Column<string>(type: "varchar(150)", nullable: false),
                    Durum = table.Column<byte>(type: "tinyint", nullable: true),
                    Mesaj = table.Column<string>(type: "varchar(500)", nullable: true),
                    SonAktifZaman = table.Column<DateTime>(type: "datetime", nullable: true),
                    SonBasariliZaman = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServisKontrol", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServisKontrol");
        }
    }
}
