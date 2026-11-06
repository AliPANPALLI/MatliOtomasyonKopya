using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KantarTakip.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddKaliteTamamlama : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "KaliteTamamlandi",
                table: "KantarGuvenlik",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "KaliteTamamlanmaZamani",
                table: "KantarGuvenlik",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KaliteTamamlayanUserId",
                table: "KantarGuvenlik",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "KaliteTamamlandi",
                table: "KantarGuvenlik");

            migrationBuilder.DropColumn(
                name: "KaliteTamamlanmaZamani",
                table: "KantarGuvenlik");

            migrationBuilder.DropColumn(
                name: "KaliteTamamlayanUserId",
                table: "KantarGuvenlik");
        }
    }
}
