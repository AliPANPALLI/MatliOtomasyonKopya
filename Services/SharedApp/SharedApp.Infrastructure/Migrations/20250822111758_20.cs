using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SharedApp.Infrastructure.Migrations
{
    public partial class _20 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserEnerjiIzlemeYetki",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LokasyonId = table.Column<int>(type: "int", nullable: false),
                    SirketId = table.Column<int>(type: "int", nullable: false),
                    Read = table.Column<bool>(type: "bit", nullable: true),
                    Write = table.Column<bool>(type: "bit", nullable: true),
                    Silindi = table.Column<bool>(type: "bit", nullable: true),
                    SilinmeZamani = table.Column<DateTime>(type: "datetime", nullable: true),
                    Aktif = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserEnerjiIzlemeYetki", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserEnerjiIzlemeYetki");
        }
    }
}
