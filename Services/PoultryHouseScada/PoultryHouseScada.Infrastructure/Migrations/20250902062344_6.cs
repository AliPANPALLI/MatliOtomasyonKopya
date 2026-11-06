using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PoultryHouseScada.Infrastructure.Migrations
{
    public partial class _6 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DonemId",
                table: "KumesHayvanLog",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "KumesId",
                table: "KumesHayvanLog",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Donem",
                type: "varchar(100)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "GuncelleyenUserId",
                table: "Donem",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "GuncellemeTarihi",
                table: "Donem",
                type: "datetime",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "GuncellemeIslemi",
                table: "Donem",
                type: "varchar(100)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Cins",
                table: "Donem",
                type: "varchar(20)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HayvanAdet",
                table: "Donem",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "SaglikDurumu",
                table: "Donem",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Yas",
                table: "Donem",
                type: "int",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DonemId",
                table: "KumesHayvanLog");

            migrationBuilder.DropColumn(
                name: "KumesId",
                table: "KumesHayvanLog");

            migrationBuilder.DropColumn(
                name: "Cins",
                table: "Donem");

            migrationBuilder.DropColumn(
                name: "HayvanAdet",
                table: "Donem");

            migrationBuilder.DropColumn(
                name: "SaglikDurumu",
                table: "Donem");

            migrationBuilder.DropColumn(
                name: "Yas",
                table: "Donem");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Donem",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "GuncelleyenUserId",
                table: "Donem",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "GuncellemeTarihi",
                table: "Donem",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "GuncellemeIslemi",
                table: "Donem",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldNullable: true);
        }
    }
}
