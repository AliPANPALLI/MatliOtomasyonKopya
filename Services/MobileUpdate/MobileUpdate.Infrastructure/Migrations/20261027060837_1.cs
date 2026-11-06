using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MobileUpdate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class _1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Cihaz",
                columns: table => new
                {
                    CihazId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    KullaniciId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    KuruluVersionCode = table.Column<int>(type: "integer", nullable: false),
                    CihazModeli = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AndroidSurumu = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    SonGorulme = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cihaz", x => x.CihazId);
                });

            migrationBuilder.CreateTable(
                name: "UygulamaSurum",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VersionCode = table.Column<int>(type: "integer", nullable: false),
                    VersionName = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DosyaAdi = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Sha256 = table.Column<string>(type: "char(64)", nullable: false),
                    Boyut = table.Column<long>(type: "bigint", nullable: false),
                    SurumNotu = table.Column<string>(type: "text", nullable: true),
                    Aktif = table.Column<bool>(type: "boolean", nullable: false),
                    YuklenmeTarihi = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    Yukleyen = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UygulamaSurum", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UygulamaSurum_VersionCode",
                table: "UygulamaSurum",
                column: "VersionCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Cihaz");

            migrationBuilder.DropTable(
                name: "UygulamaSurum");
        }
    }
}
