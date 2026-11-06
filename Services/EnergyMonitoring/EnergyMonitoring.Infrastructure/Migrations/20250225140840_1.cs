using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnergyMonitoring.Infrastructure.Migrations
{
    public partial class _1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AnalizorDegerler_Temp",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GT = table.Column<float>(type: "real", nullable: true),
                    AT = table.Column<float>(type: "real", nullable: true),
                    L1_V = table.Column<float>(type: "real", nullable: true),
                    L2_V = table.Column<float>(type: "real", nullable: true),
                    L3_V = table.Column<float>(type: "real", nullable: true),
                    L1_L2_V = table.Column<float>(type: "real", nullable: true),
                    L1_L3_V = table.Column<float>(type: "real", nullable: true),
                    L2_L3_V = table.Column<float>(type: "real", nullable: true),
                    L1_A = table.Column<float>(type: "real", nullable: true),
                    L2_A = table.Column<float>(type: "real", nullable: true),
                    L3_A = table.Column<float>(type: "real", nullable: true),
                    ToplamAkim = table.Column<float>(type: "real", nullable: true),
                    AktifGuc_L1 = table.Column<float>(type: "real", nullable: true),
                    AktifGuc_L2 = table.Column<float>(type: "real", nullable: true),
                    AktifGuc_L3 = table.Column<float>(type: "real", nullable: true),
                    AktifGuc_Toplam = table.Column<float>(type: "real", nullable: true),
                    ReaktifGuc_L1 = table.Column<float>(type: "real", nullable: true),
                    ReaktifGuc_L2 = table.Column<float>(type: "real", nullable: true),
                    ReaktifGuc_L3 = table.Column<float>(type: "real", nullable: true),
                    ReaktifGuc_Toplam = table.Column<float>(type: "real", nullable: true),
                    GorunurGuc_L1 = table.Column<float>(type: "real", nullable: true),
                    GorunurGuc_L2 = table.Column<float>(type: "real", nullable: true),
                    GorunurGuc_L3 = table.Column<float>(type: "real", nullable: true),
                    GorunurGuc_Toplam = table.Column<float>(type: "real", nullable: true),
                    GucFaktoru_L1 = table.Column<float>(type: "real", nullable: true),
                    GucFaktoru_L2 = table.Column<float>(type: "real", nullable: true),
                    GucFaktoru_L3 = table.Column<float>(type: "real", nullable: true),
                    CosFi_L1 = table.Column<float>(type: "real", nullable: true),
                    CosFi_L2 = table.Column<float>(type: "real", nullable: true),
                    CosFi_L3 = table.Column<float>(type: "real", nullable: true),
                    Frekans = table.Column<float>(type: "real", nullable: true),
                    Zaman = table.Column<DateTime>(type: "datetime", nullable: true),
                    DuringValue = table.Column<long>(type: "bigint", nullable: true),
                    PlcBagli = table.Column<bool>(type: "bit", nullable: true),
                    PlcOkunuyor = table.Column<bool>(type: "bit", nullable: true),
                    ByteDensity = table.Column<decimal>(type: "decimal(18,3)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalizorDegerler_Temp", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Cihaz",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "varchar(150)", nullable: true),
                    MakineId = table.Column<int>(type: "int", nullable: true),
                    KayitZamani = table.Column<DateTime>(type: "datetime", nullable: true),
                    Marka = table.Column<string>(type: "varchar(150)", nullable: true),
                    Model = table.Column<string>(type: "varchar(150)", nullable: true),
                    DeviceModel = table.Column<int>(type: "int", nullable: true),
                    Aktif = table.Column<bool>(type: "bit", nullable: true),
                    PlcAutoConnect = table.Column<bool>(type: "bit", nullable: true),
                    PlcIpAdres = table.Column<string>(type: "varchar(20)", nullable: true),
                    PlcPort = table.Column<int>(type: "int", nullable: true),
                    SlaveId = table.Column<byte>(type: "tinyint", nullable: true),
                    Silindi = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cihaz", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnalizorDegerler_Temp");

            migrationBuilder.DropTable(
                name: "Cihaz");
        }
    }
}
