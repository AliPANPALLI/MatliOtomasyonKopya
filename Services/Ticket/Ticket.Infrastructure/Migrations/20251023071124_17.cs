using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ticket.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class _17 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "TeklifSabitle",
                table: "TicketTanim",
                type: "bit",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TicketLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TicketId = table.Column<int>(type: "int", nullable: false),
                    YapilanDegisiklik = table.Column<string>(type: "varchar(250)", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    MatliUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    KayitZamani = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketLogs", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TicketLogs");

            migrationBuilder.DropColumn(
                name: "TeklifSabitle",
                table: "TicketTanim");
        }
    }
}
