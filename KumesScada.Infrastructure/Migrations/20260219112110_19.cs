using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KumesScada.Infrastructure.Migrations
{
    public partial class _19 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "CizimUrl",
                table: "Kumes",
                type: "varchar(350)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "CizimUrl",
                table: "Kumes",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(350)",
                oldNullable: true);
        }
    }
}
