using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ticket.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class _23 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TicketTeklifFiyatLog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TicketId = table.Column<int>(type: "int", nullable: false),
                    TeklifFiyatOncesi = table.Column<decimal>(type: "decimal(15,3)", nullable: true),
                    TeklifParaBirimiIdOncesi = table.Column<int>(type: "int", nullable: true),
                    TeklifFiyatSonrasi = table.Column<decimal>(type: "decimal(15,3)", nullable: true),
                    TeklifParaBirimiIdSonrasi = table.Column<int>(type: "int", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    MatliUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    KayitZamani = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketTeklifFiyatLog", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TicketTeklifFiyatLog");
        }
    }
}
