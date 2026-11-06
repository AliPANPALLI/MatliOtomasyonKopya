using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SharedApp.Infrastructure.Migrations
{
    public partial class _14 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeSpan>(
                name: "TekrarlamaSuresi",
                table: "AlarmKullaniciEsleme",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TekrarlansinMi",
                table: "AlarmKullaniciEsleme",
                type: "bit",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TekrarlamaSuresi",
                table: "AlarmKullaniciEsleme");

            migrationBuilder.DropColumn(
                name: "TekrarlansinMi",
                table: "AlarmKullaniciEsleme");
        }
    }
}
