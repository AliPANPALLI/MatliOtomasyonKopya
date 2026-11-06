using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WaterLineScada.Infrastructure.Migrations
{
    public partial class _13 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EtiketLog_Temp_Test",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EtiketId = table.Column<int>(type: "int", nullable: true),
                    SuKaynakId = table.Column<int>(type: "int", nullable: true),
                    SayacId = table.Column<int>(type: "int", nullable: true),
                    LokasyonId = table.Column<int>(type: "int", nullable: true),
                    KayitZamani = table.Column<DateTime>(type: "datetime", nullable: true),
                    Deger = table.Column<decimal>(type: "decimal(18,3)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EtiketLog_Temp_Test", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EtiketLog_Temp_Test");
        }
    }
}
