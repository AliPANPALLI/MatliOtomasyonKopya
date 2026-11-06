using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ticket.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class _10 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "LoginUserId",
                table: "TedarikciUser",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "FiyatDuzenleyebilsin",
                table: "TedarikciUser",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "FiyatGorsun",
                table: "TedarikciUser",
                type: "bit",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FiyatDuzenleyebilsin",
                table: "TedarikciUser");

            migrationBuilder.DropColumn(
                name: "FiyatGorsun",
                table: "TedarikciUser");

            migrationBuilder.AlterColumn<string>(
                name: "LoginUserId",
                table: "TedarikciUser",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);
        }
    }
}
