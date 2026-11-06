using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnergyMonitoring.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class _5 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnalizorDegerler");

            migrationBuilder.DropTable(
                name: "AnalizorDegerler_Temp");

            migrationBuilder.DropColumn(
                name: "DeviceModel",
                table: "Cihaz");

            migrationBuilder.DropColumn(
                name: "Marka",
                table: "Cihaz");

            migrationBuilder.DropColumn(
                name: "Model",
                table: "Cihaz");

            migrationBuilder.DropColumn(
                name: "PlcAutoConnect",
                table: "Cihaz");

            migrationBuilder.DropColumn(
                name: "PlcIpAdres",
                table: "Cihaz");

            migrationBuilder.DropColumn(
                name: "PlcPort",
                table: "Cihaz");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Makine",
                type: "varchar(100)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)");

            migrationBuilder.AlterColumn<string>(
                name: "Aciklama",
                table: "Makine",
                type: "varchar(100)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(150)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "SlaveId",
                table: "Cihaz",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(byte),
                oldType: "tinyint",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Cihaz",
                type: "varchar(100)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(150)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "MakineId",
                table: "Cihaz",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "Aktif",
                table: "Cihaz",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GatewayId",
                table: "Cihaz",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ModelId",
                table: "Cihaz",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "SilinmeZamani",
                table: "Cihaz",
                type: "datetime",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Bolum",
                type: "varchar(100)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "Gateways",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "varchar(100)", nullable: true),
                    Marka = table.Column<string>(type: "varchar(100)", nullable: true),
                    Model = table.Column<string>(type: "varchar(100)", nullable: true),
                    IpAdres = table.Column<string>(type: "varchar(20)", nullable: false),
                    Port = table.Column<string>(type: "varchar(20)", nullable: true),
                    SubnetMask = table.Column<string>(type: "varchar(20)", nullable: true),
                    RouterIp = table.Column<string>(type: "varchar(20)", nullable: true),
                    Aktif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Gateways", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Gateways");

            migrationBuilder.DropColumn(
                name: "GatewayId",
                table: "Cihaz");

            migrationBuilder.DropColumn(
                name: "ModelId",
                table: "Cihaz");

            migrationBuilder.DropColumn(
                name: "SilinmeZamani",
                table: "Cihaz");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Makine",
                type: "varchar(50)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(100)");

            migrationBuilder.AlterColumn<string>(
                name: "Aciklama",
                table: "Makine",
                type: "varchar(150)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldNullable: true);

            migrationBuilder.AlterColumn<byte>(
                name: "SlaveId",
                table: "Cihaz",
                type: "tinyint",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Cihaz",
                type: "varchar(150)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "MakineId",
                table: "Cihaz",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<bool>(
                name: "Aktif",
                table: "Cihaz",
                type: "bit",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AddColumn<int>(
                name: "DeviceModel",
                table: "Cihaz",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Marka",
                table: "Cihaz",
                type: "varchar(150)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Model",
                table: "Cihaz",
                type: "varchar(150)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PlcAutoConnect",
                table: "Cihaz",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlcIpAdres",
                table: "Cihaz",
                type: "varchar(20)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PlcPort",
                table: "Cihaz",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Bolum",
                type: "varchar(50)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "AnalizorDegerler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AT = table.Column<float>(type: "real", nullable: true),
                    AktifGuc_L1 = table.Column<float>(type: "real", nullable: true),
                    AktifGuc_L2 = table.Column<float>(type: "real", nullable: true),
                    AktifGuc_L3 = table.Column<float>(type: "real", nullable: true),
                    AktifGuc_Toplam = table.Column<float>(type: "real", nullable: true),
                    ByteDensity = table.Column<decimal>(type: "decimal(18,3)", nullable: true),
                    CihazId = table.Column<int>(type: "int", nullable: false),
                    CosFi_L1 = table.Column<float>(type: "real", nullable: true),
                    CosFi_L2 = table.Column<float>(type: "real", nullable: true),
                    CosFi_L3 = table.Column<float>(type: "real", nullable: true),
                    DuringValue = table.Column<long>(type: "bigint", nullable: true),
                    Frekans = table.Column<float>(type: "real", nullable: true),
                    GT = table.Column<float>(type: "real", nullable: true),
                    GorunurGuc_L1 = table.Column<float>(type: "real", nullable: true),
                    GorunurGuc_L2 = table.Column<float>(type: "real", nullable: true),
                    GorunurGuc_L3 = table.Column<float>(type: "real", nullable: true),
                    GorunurGuc_Toplam = table.Column<float>(type: "real", nullable: true),
                    GucFaktoru_L1 = table.Column<float>(type: "real", nullable: true),
                    GucFaktoru_L2 = table.Column<float>(type: "real", nullable: true),
                    GucFaktoru_L3 = table.Column<float>(type: "real", nullable: true),
                    L1_A = table.Column<float>(type: "real", nullable: true),
                    L1_L2_V = table.Column<float>(type: "real", nullable: true),
                    L1_L3_V = table.Column<float>(type: "real", nullable: true),
                    L1_V = table.Column<float>(type: "real", nullable: true),
                    L2_A = table.Column<float>(type: "real", nullable: true),
                    L2_L3_V = table.Column<float>(type: "real", nullable: true),
                    L2_V = table.Column<float>(type: "real", nullable: true),
                    L3_A = table.Column<float>(type: "real", nullable: true),
                    L3_V = table.Column<float>(type: "real", nullable: true),
                    PlcBagli = table.Column<bool>(type: "bit", nullable: true),
                    PlcOkunuyor = table.Column<bool>(type: "bit", nullable: true),
                    ReaktifGuc_L1 = table.Column<float>(type: "real", nullable: true),
                    ReaktifGuc_L2 = table.Column<float>(type: "real", nullable: true),
                    ReaktifGuc_L3 = table.Column<float>(type: "real", nullable: true),
                    ReaktifGuc_Toplam = table.Column<float>(type: "real", nullable: true),
                    ToplamAkim = table.Column<float>(type: "real", nullable: true),
                    Zaman = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalizorDegerler", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AnalizorDegerler_Temp",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AT = table.Column<float>(type: "real", nullable: true),
                    AktifGuc_L1 = table.Column<float>(type: "real", nullable: true),
                    AktifGuc_L2 = table.Column<float>(type: "real", nullable: true),
                    AktifGuc_L3 = table.Column<float>(type: "real", nullable: true),
                    AktifGuc_Toplam = table.Column<float>(type: "real", nullable: true),
                    ByteDensity = table.Column<decimal>(type: "decimal(18,3)", nullable: true),
                    CihazId = table.Column<int>(type: "int", nullable: false),
                    CosFi_L1 = table.Column<float>(type: "real", nullable: true),
                    CosFi_L2 = table.Column<float>(type: "real", nullable: true),
                    CosFi_L3 = table.Column<float>(type: "real", nullable: true),
                    DuringValue = table.Column<long>(type: "bigint", nullable: true),
                    Frekans = table.Column<float>(type: "real", nullable: true),
                    GT = table.Column<float>(type: "real", nullable: true),
                    GorunurGuc_L1 = table.Column<float>(type: "real", nullable: true),
                    GorunurGuc_L2 = table.Column<float>(type: "real", nullable: true),
                    GorunurGuc_L3 = table.Column<float>(type: "real", nullable: true),
                    GorunurGuc_Toplam = table.Column<float>(type: "real", nullable: true),
                    GucFaktoru_L1 = table.Column<float>(type: "real", nullable: true),
                    GucFaktoru_L2 = table.Column<float>(type: "real", nullable: true),
                    GucFaktoru_L3 = table.Column<float>(type: "real", nullable: true),
                    L1_A = table.Column<float>(type: "real", nullable: true),
                    L1_L2_V = table.Column<float>(type: "real", nullable: true),
                    L1_L3_V = table.Column<float>(type: "real", nullable: true),
                    L1_V = table.Column<float>(type: "real", nullable: true),
                    L2_A = table.Column<float>(type: "real", nullable: true),
                    L2_L3_V = table.Column<float>(type: "real", nullable: true),
                    L2_V = table.Column<float>(type: "real", nullable: true),
                    L3_A = table.Column<float>(type: "real", nullable: true),
                    L3_V = table.Column<float>(type: "real", nullable: true),
                    PlcBagli = table.Column<bool>(type: "bit", nullable: true),
                    PlcOkunuyor = table.Column<bool>(type: "bit", nullable: true),
                    ReaktifGuc_L1 = table.Column<float>(type: "real", nullable: true),
                    ReaktifGuc_L2 = table.Column<float>(type: "real", nullable: true),
                    ReaktifGuc_L3 = table.Column<float>(type: "real", nullable: true),
                    ReaktifGuc_Toplam = table.Column<float>(type: "real", nullable: true),
                    ToplamAkim = table.Column<float>(type: "real", nullable: true),
                    Zaman = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalizorDegerler_Temp", x => x.Id);
                });
        }
    }
}
