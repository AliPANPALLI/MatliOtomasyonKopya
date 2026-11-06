using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ticket.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class _19 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SasNo",
                table: "TicketTanim",
                type: "varchar(50)",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "TeklifKararDurumu",
                table: "TicketTanim",
                type: "tinyint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SasNo",
                table: "TicketTanim");

            migrationBuilder.DropColumn(
                name: "TeklifKararDurumu",
                table: "TicketTanim");
        }
    }
}
