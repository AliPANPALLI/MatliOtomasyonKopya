using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PoultryHouseScada.Infrastructure.Migrations
{
    public partial class _16 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AsiriKucukAdet",
                table: "KumesLog",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AzDiskiliYumurtaAdet",
                table: "KumesLog",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AzKanliYumurtaAdet",
                table: "KumesLog",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AzKirliYumurtaAdet",
                table: "KumesLog",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CokDiskiliYumurtaAdet",
                table: "KumesLog",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CokKanliYumurtaAdet",
                table: "KumesLog",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CokKirliYumurtaAdet",
                table: "KumesLog",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HicDiskiliYumurtaAdet",
                table: "KumesLog",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HicKanliYumurtaAdet",
                table: "KumesLog",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HicKirliYumurtaAdet",
                table: "KumesLog",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "KirikYumurtaAdet",
                table: "KumesLog",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LAdet",
                table: "KumesLog",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MAdet",
                table: "KumesLog",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OrtaDiskiliYumurtaAdet",
                table: "KumesLog",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OrtaKanliYumurtaAdet",
                table: "KumesLog",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OrtaKirliYumurtaAdet",
                table: "KumesLog",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SAdet",
                table: "KumesLog",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "XLAdet",
                table: "KumesLog",
                type: "int",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AsiriKucukAdet",
                table: "KumesLog");

            migrationBuilder.DropColumn(
                name: "AzDiskiliYumurtaAdet",
                table: "KumesLog");

            migrationBuilder.DropColumn(
                name: "AzKanliYumurtaAdet",
                table: "KumesLog");

            migrationBuilder.DropColumn(
                name: "AzKirliYumurtaAdet",
                table: "KumesLog");

            migrationBuilder.DropColumn(
                name: "CokDiskiliYumurtaAdet",
                table: "KumesLog");

            migrationBuilder.DropColumn(
                name: "CokKanliYumurtaAdet",
                table: "KumesLog");

            migrationBuilder.DropColumn(
                name: "CokKirliYumurtaAdet",
                table: "KumesLog");

            migrationBuilder.DropColumn(
                name: "HicDiskiliYumurtaAdet",
                table: "KumesLog");

            migrationBuilder.DropColumn(
                name: "HicKanliYumurtaAdet",
                table: "KumesLog");

            migrationBuilder.DropColumn(
                name: "HicKirliYumurtaAdet",
                table: "KumesLog");

            migrationBuilder.DropColumn(
                name: "KirikYumurtaAdet",
                table: "KumesLog");

            migrationBuilder.DropColumn(
                name: "LAdet",
                table: "KumesLog");

            migrationBuilder.DropColumn(
                name: "MAdet",
                table: "KumesLog");

            migrationBuilder.DropColumn(
                name: "OrtaDiskiliYumurtaAdet",
                table: "KumesLog");

            migrationBuilder.DropColumn(
                name: "OrtaKanliYumurtaAdet",
                table: "KumesLog");

            migrationBuilder.DropColumn(
                name: "OrtaKirliYumurtaAdet",
                table: "KumesLog");

            migrationBuilder.DropColumn(
                name: "SAdet",
                table: "KumesLog");

            migrationBuilder.DropColumn(
                name: "XLAdet",
                table: "KumesLog");
        }
    }
}
