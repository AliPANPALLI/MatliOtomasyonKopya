using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SuHatScada.Infrastructure.Migrations
{
    public partial class _23 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CizimTipi",
                table: "Cizim",
                type: "varchar(50)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CizimTipi",
                table: "Cizim");
        }
    }
}
