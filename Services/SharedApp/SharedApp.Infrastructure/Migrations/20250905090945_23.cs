using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SharedApp.Infrastructure.Migrations
{
    public partial class _23 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "BaslamaZamani",
                table: "MesajGonderimTakip",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "BitisZamani",
                table: "MesajGonderimTakip",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CallId",
                table: "MesajGonderimTakip",
                type: "varchar(500)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DinlemeSuresi",
                table: "MesajGonderimTakip",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DurumAciklama",
                table: "MesajGonderimTakip",
                type: "varchar(500)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DurumKodu",
                table: "MesajGonderimTakip",
                type: "varchar(500)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BaslamaZamani",
                table: "MesajGonderimTakip");

            migrationBuilder.DropColumn(
                name: "BitisZamani",
                table: "MesajGonderimTakip");

            migrationBuilder.DropColumn(
                name: "CallId",
                table: "MesajGonderimTakip");

            migrationBuilder.DropColumn(
                name: "DinlemeSuresi",
                table: "MesajGonderimTakip");

            migrationBuilder.DropColumn(
                name: "DurumAciklama",
                table: "MesajGonderimTakip");

            migrationBuilder.DropColumn(
                name: "DurumKodu",
                table: "MesajGonderimTakip");
        }
    }
}
