using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ticket.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class _13 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DurumIdTedarikci",
                table: "TicketTanim",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DurumIdTedarikciOncesi",
                table: "TicketDurumLog",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DurumIdTedarikciSonrasi",
                table: "TicketDurumLog",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "TakipciGorsun",
                table: "TicketDurum",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TedarikciGorsun",
                table: "TicketDurum",
                type: "bit",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DurumIdTedarikci",
                table: "TicketTanim");

            migrationBuilder.DropColumn(
                name: "DurumIdTedarikciOncesi",
                table: "TicketDurumLog");

            migrationBuilder.DropColumn(
                name: "DurumIdTedarikciSonrasi",
                table: "TicketDurumLog");

            migrationBuilder.DropColumn(
                name: "TakipciGorsun",
                table: "TicketDurum");

            migrationBuilder.DropColumn(
                name: "TedarikciGorsun",
                table: "TicketDurum");
        }
    }
}
