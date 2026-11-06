using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SharedApp.Infrastructure.Migrations
{
    public partial class _5 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Aktif",
                table: "AlarmKullaniciEsleme",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SilinmeZamani",
                table: "AlarmKullaniciEsleme",
                type: "datetime",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Aktif",
                table: "AlarmKullaniciEsleme");

            migrationBuilder.DropColumn(
                name: "SilinmeZamani",
                table: "AlarmKullaniciEsleme");
        }
    }
}
