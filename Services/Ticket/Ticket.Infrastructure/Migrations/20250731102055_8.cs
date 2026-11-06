using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ticket.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class _8 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MatliUserId",
                table: "TedarikciUser",
                newName: "LoginUserId");

            migrationBuilder.AddColumn<string>(
                name: "LoginUserId",
                table: "TicketAciklamaEsleme",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LoginUserId",
                table: "TicketAciklamaEsleme");

            migrationBuilder.RenameColumn(
                name: "LoginUserId",
                table: "TedarikciUser",
                newName: "MatliUserId");
        }
    }
}
