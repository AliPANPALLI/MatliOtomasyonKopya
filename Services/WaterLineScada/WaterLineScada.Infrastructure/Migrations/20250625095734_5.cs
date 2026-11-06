using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WaterLineScada.Infrastructure.Migrations
{
    public partial class _5 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ModTuru",
                table: "SekilEtiketKural",
                type: "varchar(20)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ModTuru",
                table: "SekilEtiketKural");
        }
    }
}
