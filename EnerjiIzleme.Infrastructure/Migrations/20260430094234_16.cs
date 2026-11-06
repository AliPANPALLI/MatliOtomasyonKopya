using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnerjiIzleme.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class _16 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EklenmeZamani",
                table: "Makine",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EkleyenUserId",
                table: "Makine",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "GuncellemeZamani",
                table: "Makine",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GuncelleyenUserId",
                table: "Makine",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EklenmeZamani",
                table: "Cihaz",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EkleyenUserId",
                table: "Cihaz",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "GuncellemeZamani",
                table: "Cihaz",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GuncelleyenUserId",
                table: "Cihaz",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EklenmeZamani",
                table: "Bolum",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EkleyenUserId",
                table: "Bolum",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "GuncellemeZamani",
                table: "Bolum",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GuncelleyenUserId",
                table: "Bolum",
                type: "nvarchar(450)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EklenmeZamani",
                table: "Makine");

            migrationBuilder.DropColumn(
                name: "EkleyenUserId",
                table: "Makine");

            migrationBuilder.DropColumn(
                name: "GuncellemeZamani",
                table: "Makine");

            migrationBuilder.DropColumn(
                name: "GuncelleyenUserId",
                table: "Makine");

            migrationBuilder.DropColumn(
                name: "EklenmeZamani",
                table: "Cihaz");

            migrationBuilder.DropColumn(
                name: "EkleyenUserId",
                table: "Cihaz");

            migrationBuilder.DropColumn(
                name: "GuncellemeZamani",
                table: "Cihaz");

            migrationBuilder.DropColumn(
                name: "GuncelleyenUserId",
                table: "Cihaz");

            migrationBuilder.DropColumn(
                name: "EklenmeZamani",
                table: "Bolum");

            migrationBuilder.DropColumn(
                name: "EkleyenUserId",
                table: "Bolum");

            migrationBuilder.DropColumn(
                name: "GuncellemeZamani",
                table: "Bolum");

            migrationBuilder.DropColumn(
                name: "GuncelleyenUserId",
                table: "Bolum");
        }
    }
}
