using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SharedApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class _45 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "YanginAlgilamaLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AlarmTip = table.Column<string>(type: "varchar(30)", nullable: true),
                    KameraNo = table.Column<string>(type: "varchar(30)", nullable: true),
                    KayitZamani = table.Column<DateTime>(type: "datetime", nullable: true),
                    NodeRedKayitZamani = table.Column<DateTime>(type: "datetime", nullable: true),
                    Silindi = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_YanginAlgilamaLogs", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "YanginAlgilamaLogs");
        }
    }
}
