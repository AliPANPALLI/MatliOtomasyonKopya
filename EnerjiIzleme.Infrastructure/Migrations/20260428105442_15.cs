using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnerjiIzleme.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class _15 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Onaylandi",
                table: "Makine",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OnaylanmaZamani",
                table: "Makine",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OnaylayanUserId",
                table: "Makine",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Onaylandi",
                table: "Cihaz",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OnaylanmaZamani",
                table: "Cihaz",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OnaylayanUserId",
                table: "Cihaz",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Onaylandi",
                table: "Bolum",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OnaylanmaZamani",
                table: "Bolum",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OnaylayanUserId",
                table: "Bolum",
                type: "nvarchar(450)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Onaylandi",
                table: "Makine");

            migrationBuilder.DropColumn(
                name: "OnaylanmaZamani",
                table: "Makine");

            migrationBuilder.DropColumn(
                name: "OnaylayanUserId",
                table: "Makine");

            migrationBuilder.DropColumn(
                name: "Onaylandi",
                table: "Cihaz");

            migrationBuilder.DropColumn(
                name: "OnaylanmaZamani",
                table: "Cihaz");

            migrationBuilder.DropColumn(
                name: "OnaylayanUserId",
                table: "Cihaz");

            migrationBuilder.DropColumn(
                name: "Onaylandi",
                table: "Bolum");

            migrationBuilder.DropColumn(
                name: "OnaylanmaZamani",
                table: "Bolum");

            migrationBuilder.DropColumn(
                name: "OnaylayanUserId",
                table: "Bolum");
        }
    }
}
