using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SharedApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class _41 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AlarmLog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KayitZamani = table.Column<DateTime>(type: "datetime", nullable: true),
                    Mesaj = table.Column<string>(type: "varchar(max)", nullable: true),
                    AlarmId = table.Column<int>(type: "int", nullable: true),
                    AlarmSeviye = table.Column<byte>(type: "tinyint", nullable: true),
                    Aktif = table.Column<bool>(type: "bit", nullable: true),
                    PasifZamani = table.Column<DateTime>(type: "datetime", nullable: true),
                    Silindi = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlarmLog", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlarmLog");
        }
    }
}
