using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SharedApp.Infrastructure.Migrations
{
    public partial class _9 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeSpan>(
                name: "BildirimSuresi",
                table: "AlarmKullaniciEsleme",
                type: "time",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<string>(
                name: "BildirimYontemi",
                table: "AlarmKullaniciEsleme",
                type: "varchar(15)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SeviyeAktif",
                table: "AlarmKullaniciEsleme",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SeviyeNo",
                table: "AlarmKullaniciEsleme",
                type: "int",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BildirimSuresi",
                table: "AlarmKullaniciEsleme");

            migrationBuilder.DropColumn(
                name: "BildirimYontemi",
                table: "AlarmKullaniciEsleme");

            migrationBuilder.DropColumn(
                name: "SeviyeAktif",
                table: "AlarmKullaniciEsleme");

            migrationBuilder.DropColumn(
                name: "SeviyeNo",
                table: "AlarmKullaniciEsleme");
        }
    }
}
