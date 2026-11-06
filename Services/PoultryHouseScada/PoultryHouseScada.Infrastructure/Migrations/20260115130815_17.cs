using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PoultryHouseScada.Infrastructure.Migrations
{
    public partial class _17 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AsiriKucukAdet",
                table: "KumesLogYeni",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AzDiskiliYumurtaAdet",
                table: "KumesLogYeni",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AzKanliYumurtaAdet",
                table: "KumesLogYeni",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AzKirliYumurtaAdet",
                table: "KumesLogYeni",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CokDiskiliYumurtaAdet",
                table: "KumesLogYeni",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CokKanliYumurtaAdet",
                table: "KumesLogYeni",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CokKirliYumurtaAdet",
                table: "KumesLogYeni",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HicDiskiliYumurtaAdet",
                table: "KumesLogYeni",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HicKanliYumurtaAdet",
                table: "KumesLogYeni",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HicKirliYumurtaAdet",
                table: "KumesLogYeni",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "KirikYumurtaAdet",
                table: "KumesLogYeni",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LAdet",
                table: "KumesLogYeni",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MAdet",
                table: "KumesLogYeni",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OrtaDiskiliYumurtaAdet",
                table: "KumesLogYeni",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OrtaKanliYumurtaAdet",
                table: "KumesLogYeni",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OrtaKirliYumurtaAdet",
                table: "KumesLogYeni",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SAdet",
                table: "KumesLogYeni",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "XLAdet",
                table: "KumesLogYeni",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AsiriKucukAdet",
                table: "KumesLog_TempYeni",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AzDiskiliYumurtaAdet",
                table: "KumesLog_TempYeni",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AzKanliYumurtaAdet",
                table: "KumesLog_TempYeni",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AzKirliYumurtaAdet",
                table: "KumesLog_TempYeni",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CokDiskiliYumurtaAdet",
                table: "KumesLog_TempYeni",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CokKanliYumurtaAdet",
                table: "KumesLog_TempYeni",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CokKirliYumurtaAdet",
                table: "KumesLog_TempYeni",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HicDiskiliYumurtaAdet",
                table: "KumesLog_TempYeni",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HicKanliYumurtaAdet",
                table: "KumesLog_TempYeni",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HicKirliYumurtaAdet",
                table: "KumesLog_TempYeni",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "KirikYumurtaAdet",
                table: "KumesLog_TempYeni",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LAdet",
                table: "KumesLog_TempYeni",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MAdet",
                table: "KumesLog_TempYeni",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OrtaDiskiliYumurtaAdet",
                table: "KumesLog_TempYeni",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OrtaKanliYumurtaAdet",
                table: "KumesLog_TempYeni",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OrtaKirliYumurtaAdet",
                table: "KumesLog_TempYeni",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SAdet",
                table: "KumesLog_TempYeni",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "XLAdet",
                table: "KumesLog_TempYeni",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AsiriKucukAdet",
                table: "KumesLog_Temp",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AzDiskiliYumurtaAdet",
                table: "KumesLog_Temp",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AzKanliYumurtaAdet",
                table: "KumesLog_Temp",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AzKirliYumurtaAdet",
                table: "KumesLog_Temp",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CokDiskiliYumurtaAdet",
                table: "KumesLog_Temp",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CokKanliYumurtaAdet",
                table: "KumesLog_Temp",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CokKirliYumurtaAdet",
                table: "KumesLog_Temp",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HicDiskiliYumurtaAdet",
                table: "KumesLog_Temp",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HicKanliYumurtaAdet",
                table: "KumesLog_Temp",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HicKirliYumurtaAdet",
                table: "KumesLog_Temp",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "KirikYumurtaAdet",
                table: "KumesLog_Temp",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LAdet",
                table: "KumesLog_Temp",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MAdet",
                table: "KumesLog_Temp",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OrtaDiskiliYumurtaAdet",
                table: "KumesLog_Temp",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OrtaKanliYumurtaAdet",
                table: "KumesLog_Temp",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OrtaKirliYumurtaAdet",
                table: "KumesLog_Temp",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SAdet",
                table: "KumesLog_Temp",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "XLAdet",
                table: "KumesLog_Temp",
                type: "int",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AsiriKucukAdet",
                table: "KumesLogYeni");

            migrationBuilder.DropColumn(
                name: "AzDiskiliYumurtaAdet",
                table: "KumesLogYeni");

            migrationBuilder.DropColumn(
                name: "AzKanliYumurtaAdet",
                table: "KumesLogYeni");

            migrationBuilder.DropColumn(
                name: "AzKirliYumurtaAdet",
                table: "KumesLogYeni");

            migrationBuilder.DropColumn(
                name: "CokDiskiliYumurtaAdet",
                table: "KumesLogYeni");

            migrationBuilder.DropColumn(
                name: "CokKanliYumurtaAdet",
                table: "KumesLogYeni");

            migrationBuilder.DropColumn(
                name: "CokKirliYumurtaAdet",
                table: "KumesLogYeni");

            migrationBuilder.DropColumn(
                name: "HicDiskiliYumurtaAdet",
                table: "KumesLogYeni");

            migrationBuilder.DropColumn(
                name: "HicKanliYumurtaAdet",
                table: "KumesLogYeni");

            migrationBuilder.DropColumn(
                name: "HicKirliYumurtaAdet",
                table: "KumesLogYeni");

            migrationBuilder.DropColumn(
                name: "KirikYumurtaAdet",
                table: "KumesLogYeni");

            migrationBuilder.DropColumn(
                name: "LAdet",
                table: "KumesLogYeni");

            migrationBuilder.DropColumn(
                name: "MAdet",
                table: "KumesLogYeni");

            migrationBuilder.DropColumn(
                name: "OrtaDiskiliYumurtaAdet",
                table: "KumesLogYeni");

            migrationBuilder.DropColumn(
                name: "OrtaKanliYumurtaAdet",
                table: "KumesLogYeni");

            migrationBuilder.DropColumn(
                name: "OrtaKirliYumurtaAdet",
                table: "KumesLogYeni");

            migrationBuilder.DropColumn(
                name: "SAdet",
                table: "KumesLogYeni");

            migrationBuilder.DropColumn(
                name: "XLAdet",
                table: "KumesLogYeni");

            migrationBuilder.DropColumn(
                name: "AsiriKucukAdet",
                table: "KumesLog_TempYeni");

            migrationBuilder.DropColumn(
                name: "AzDiskiliYumurtaAdet",
                table: "KumesLog_TempYeni");

            migrationBuilder.DropColumn(
                name: "AzKanliYumurtaAdet",
                table: "KumesLog_TempYeni");

            migrationBuilder.DropColumn(
                name: "AzKirliYumurtaAdet",
                table: "KumesLog_TempYeni");

            migrationBuilder.DropColumn(
                name: "CokDiskiliYumurtaAdet",
                table: "KumesLog_TempYeni");

            migrationBuilder.DropColumn(
                name: "CokKanliYumurtaAdet",
                table: "KumesLog_TempYeni");

            migrationBuilder.DropColumn(
                name: "CokKirliYumurtaAdet",
                table: "KumesLog_TempYeni");

            migrationBuilder.DropColumn(
                name: "HicDiskiliYumurtaAdet",
                table: "KumesLog_TempYeni");

            migrationBuilder.DropColumn(
                name: "HicKanliYumurtaAdet",
                table: "KumesLog_TempYeni");

            migrationBuilder.DropColumn(
                name: "HicKirliYumurtaAdet",
                table: "KumesLog_TempYeni");

            migrationBuilder.DropColumn(
                name: "KirikYumurtaAdet",
                table: "KumesLog_TempYeni");

            migrationBuilder.DropColumn(
                name: "LAdet",
                table: "KumesLog_TempYeni");

            migrationBuilder.DropColumn(
                name: "MAdet",
                table: "KumesLog_TempYeni");

            migrationBuilder.DropColumn(
                name: "OrtaDiskiliYumurtaAdet",
                table: "KumesLog_TempYeni");

            migrationBuilder.DropColumn(
                name: "OrtaKanliYumurtaAdet",
                table: "KumesLog_TempYeni");

            migrationBuilder.DropColumn(
                name: "OrtaKirliYumurtaAdet",
                table: "KumesLog_TempYeni");

            migrationBuilder.DropColumn(
                name: "SAdet",
                table: "KumesLog_TempYeni");

            migrationBuilder.DropColumn(
                name: "XLAdet",
                table: "KumesLog_TempYeni");

            migrationBuilder.DropColumn(
                name: "AsiriKucukAdet",
                table: "KumesLog_Temp");

            migrationBuilder.DropColumn(
                name: "AzDiskiliYumurtaAdet",
                table: "KumesLog_Temp");

            migrationBuilder.DropColumn(
                name: "AzKanliYumurtaAdet",
                table: "KumesLog_Temp");

            migrationBuilder.DropColumn(
                name: "AzKirliYumurtaAdet",
                table: "KumesLog_Temp");

            migrationBuilder.DropColumn(
                name: "CokDiskiliYumurtaAdet",
                table: "KumesLog_Temp");

            migrationBuilder.DropColumn(
                name: "CokKanliYumurtaAdet",
                table: "KumesLog_Temp");

            migrationBuilder.DropColumn(
                name: "CokKirliYumurtaAdet",
                table: "KumesLog_Temp");

            migrationBuilder.DropColumn(
                name: "HicDiskiliYumurtaAdet",
                table: "KumesLog_Temp");

            migrationBuilder.DropColumn(
                name: "HicKanliYumurtaAdet",
                table: "KumesLog_Temp");

            migrationBuilder.DropColumn(
                name: "HicKirliYumurtaAdet",
                table: "KumesLog_Temp");

            migrationBuilder.DropColumn(
                name: "KirikYumurtaAdet",
                table: "KumesLog_Temp");

            migrationBuilder.DropColumn(
                name: "LAdet",
                table: "KumesLog_Temp");

            migrationBuilder.DropColumn(
                name: "MAdet",
                table: "KumesLog_Temp");

            migrationBuilder.DropColumn(
                name: "OrtaDiskiliYumurtaAdet",
                table: "KumesLog_Temp");

            migrationBuilder.DropColumn(
                name: "OrtaKanliYumurtaAdet",
                table: "KumesLog_Temp");

            migrationBuilder.DropColumn(
                name: "OrtaKirliYumurtaAdet",
                table: "KumesLog_Temp");

            migrationBuilder.DropColumn(
                name: "SAdet",
                table: "KumesLog_Temp");

            migrationBuilder.DropColumn(
                name: "XLAdet",
                table: "KumesLog_Temp");
        }
    }
}
