using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ticket.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class _20 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BackColor",
                table: "TicketDurum",
                type: "varchar(20)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DurumKey",
                table: "TicketDurum",
                type: "varchar(50)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TextColor",
                table: "TicketDurum",
                type: "varchar(20)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BackColor",
                table: "TicketDurum");

            migrationBuilder.DropColumn(
                name: "DurumKey",
                table: "TicketDurum");

            migrationBuilder.DropColumn(
                name: "TextColor",
                table: "TicketDurum");
        }
    }
}
