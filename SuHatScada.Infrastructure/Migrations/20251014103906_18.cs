using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SuHatScada.Infrastructure.Migrations
{
    public partial class _18 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EtiketIdSayacDegeri",
                table: "Sayac",
                type: "int",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EtiketIdSayacDegeri",
                table: "Sayac");
        }
    }
}
