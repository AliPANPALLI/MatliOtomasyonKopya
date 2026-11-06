using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SharedApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class _44 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "GuncellemeZamani",
                table: "AlarmLog",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AlarmEslemeSozcugu",
                table: "Alarmlar",
                type: "varchar(50)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FizikselLokasyonId",
                table: "Alarmlar",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GuncellemeZamani",
                table: "AlarmLog");

            migrationBuilder.DropColumn(
                name: "AlarmEslemeSozcugu",
                table: "Alarmlar");

            migrationBuilder.DropColumn(
                name: "FizikselLokasyonId",
                table: "Alarmlar");
        }
    }
}
