using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ticket.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class _16 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "GuncellemeZamani",
                table: "TicketTanim",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GuncellemedeYapilanIslem",
                table: "TicketTanim",
                type: "varchar(50)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GuncelleyenUserId",
                table: "TicketTanim",
                type: "nvarchar(450)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GuncellemeZamani",
                table: "TicketTanim");

            migrationBuilder.DropColumn(
                name: "GuncellemedeYapilanIslem",
                table: "TicketTanim");

            migrationBuilder.DropColumn(
                name: "GuncelleyenUserId",
                table: "TicketTanim");
        }
    }
}
