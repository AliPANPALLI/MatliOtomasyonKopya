using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SharedApp.Infrastructure.Migrations
{
    public partial class _13 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CizimId",
                table: "UserSuKaynakYetki",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CizimId",
                table: "FormLokasyonEsleme",
                type: "int",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CizimId",
                table: "UserSuKaynakYetki");

            migrationBuilder.DropColumn(
                name: "CizimId",
                table: "FormLokasyonEsleme");
        }
    }
}
