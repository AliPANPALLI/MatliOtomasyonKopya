using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvironmentMonitoringScada.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class _1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Bolum",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LokasyonId = table.Column<int>(type: "int", nullable: false),
                    BolumAdi = table.Column<string>(type: "varchar(50)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bolum", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CihazLog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OdaId = table.Column<int>(type: "int", nullable: false),
                    KayitZamani = table.Column<DateTime>(type: "datetime", nullable: true),
                    SN_1_Connected = table.Column<bool>(type: "bit", nullable: true),
                    SN_1_Flag = table.Column<bool>(type: "bit", nullable: true),
                    SN_1_Sicaklik = table.Column<float>(type: "real", nullable: true),
                    SN_1_Nem = table.Column<float>(type: "real", nullable: true),
                    SN_1_Battery = table.Column<int>(type: "int", nullable: true),
                    SN_1_LastSeen = table.Column<DateTime>(type: "datetime", nullable: true),
                    SN_2_Connected = table.Column<bool>(type: "bit", nullable: true),
                    SN_2_Flag = table.Column<bool>(type: "bit", nullable: true),
                    SN_2_Sicaklik = table.Column<float>(type: "real", nullable: true),
                    SN_2_Nem = table.Column<float>(type: "real", nullable: true),
                    SN_2_Battery = table.Column<int>(type: "int", nullable: true),
                    SN_2_LastSeen = table.Column<DateTime>(type: "datetime", nullable: true),
                    SB_1_Connected = table.Column<bool>(type: "bit", nullable: true),
                    SB_1_Flag = table.Column<bool>(type: "bit", nullable: true),
                    SB_1_SuVar = table.Column<bool>(type: "bit", nullable: true),
                    SB_1_Battery = table.Column<int>(type: "int", nullable: true),
                    SB_1_LastSeen = table.Column<DateTime>(type: "datetime", nullable: true),
                    SB_2_Connected = table.Column<bool>(type: "bit", nullable: true),
                    SB_2_Flag = table.Column<bool>(type: "bit", nullable: true),
                    SB_2_SuVar = table.Column<bool>(type: "bit", nullable: true),
                    SB_2_Battery = table.Column<int>(type: "int", nullable: true),
                    SB_2_LastSeen = table.Column<DateTime>(type: "datetime", nullable: true),
                    HS_1_Connected = table.Column<bool>(type: "bit", nullable: true),
                    HS_1_Flag = table.Column<bool>(type: "bit", nullable: true),
                    HS_1_HaraketVar = table.Column<bool>(type: "bit", nullable: true),
                    HS_1_LastSeen = table.Column<DateTime>(type: "datetime", nullable: true),
                    KS_1_Connected = table.Column<bool>(type: "bit", nullable: true),
                    KS_1_Flag = table.Column<bool>(type: "bit", nullable: true),
                    KS_1_KapiAcildi = table.Column<bool>(type: "bit", nullable: true),
                    KS_1_LastSeen = table.Column<DateTime>(type: "datetime", nullable: true),
                    KS_1_Battery = table.Column<int>(type: "int", nullable: true),
                    EA_L1_V = table.Column<float>(type: "real", nullable: true),
                    EA_L1_A = table.Column<float>(type: "real", nullable: true),
                    EA_L2_V = table.Column<float>(type: "real", nullable: true),
                    EA_L2_A = table.Column<float>(type: "real", nullable: true),
                    EA_L3_V = table.Column<float>(type: "real", nullable: true),
                    EA_L3_A = table.Column<float>(type: "real", nullable: true),
                    EA_PE_V = table.Column<float>(type: "real", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CihazLog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Oda",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BolumId = table.Column<int>(type: "int", nullable: false),
                    OdaAdi = table.Column<string>(type: "varchar(50)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Oda", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Oda_Bolum_BolumId",
                        column: x => x.BolumId,
                        principalTable: "Bolum",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Cihaz",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OdaId = table.Column<int>(type: "int", nullable: false),
                    SensorIdMQTT = table.Column<int>(type: "int", nullable: false),
                    Marka = table.Column<string>(type: "varchar(50)", nullable: true),
                    Model = table.Column<string>(type: "varchar(50)", nullable: true),
                    SensorAdi = table.Column<string>(type: "varchar(50)", nullable: true),
                    KayitZamani = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cihaz", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cihaz_Oda_OdaId",
                        column: x => x.OdaId,
                        principalTable: "Oda",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cihaz_OdaId",
                table: "Cihaz",
                column: "OdaId");

            migrationBuilder.CreateIndex(
                name: "IX_Oda_BolumId",
                table: "Oda",
                column: "BolumId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Cihaz");

            migrationBuilder.DropTable(
                name: "CihazLog");

            migrationBuilder.DropTable(
                name: "Oda");

            migrationBuilder.DropTable(
                name: "Bolum");
        }
    }
}
