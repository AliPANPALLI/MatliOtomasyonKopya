using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ticket.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class _22 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "TeklifKararDurumGuncellemeZamani",
                table: "TicketTanim",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TeklifKararDurumGuncelleyenUserId",
                table: "TicketTanim",
                type: "nvarchar(450)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TeklifKararDurumGuncellemeZamani",
                table: "TicketTanim");

            migrationBuilder.DropColumn(
                name: "TeklifKararDurumGuncelleyenUserId",
                table: "TicketTanim");
        }
    }
}
