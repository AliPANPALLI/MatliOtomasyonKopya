using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnerjiIzleme.Infrastructure.Migrations
{
    public partial class _4 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CihazId",
                table: "AnalizorDegerler_Temp",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "AnalizorDegerler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CihazId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_AnalizorDegerler", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnalizorDegerler");

            migrationBuilder.DropColumn(
                name: "CihazId",
                table: "AnalizorDegerler_Temp");
        }
    }
}
