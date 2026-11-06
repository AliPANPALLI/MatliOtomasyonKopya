using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KantarTakip.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TeslimFisiVePlakaTakibi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Boceklenme",
                table: "KantarKalite",
                type: "nvarchar(100)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Curuk",
                table: "KantarKalite",
                type: "nvarchar(100)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Koku",
                table: "KantarKalite",
                type: "nvarchar(100)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Kuf",
                table: "KantarKalite",
                type: "nvarchar(100)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Sonuc",
                table: "KantarKalite",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SonucAciklama",
                table: "KantarKalite",
                type: "nvarchar(500)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tozluluk",
                table: "KantarKalite",
                type: "nvarchar(100)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BosaltimYapanUserId",
                table: "KantarGuvenlik",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "BosaltimZamani",
                table: "KantarGuvenlik",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DokulduguYer",
                table: "KantarGuvenlik",
                type: "nvarchar(200)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IrsaliyeNo",
                table: "KantarGuvenlik",
                type: "nvarchar(50)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "KaliteSonucu",
                table: "KantarGuvenlik",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlakaNormalized",
                table: "KantarGuvenlik",
                type: "varchar(100)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SonUrun",
                table: "KantarGuvenlik",
                type: "nvarchar(200)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Tarih",
                table: "KantarGuvenlik",
                type: "date",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "UrunAdi",
                table: "KantarGuvenlik",
                type: "nvarchar(200)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ZiyaretciKayitId",
                table: "KantarGuvenlik",
                type: "nvarchar(50)",
                nullable: true);

            // Mevcut kayıtlar için veri doldurma.
            // EXEC: script tek batch çalıştırılırsa yeni kolonlar derleme
            // anında henüz yok sayılmasın diye.

            // Tarih: sistem giriş zamanından.
            migrationBuilder.Sql(@"
EXEC('
UPDATE KantarGuvenlik
SET Tarih = CAST(COALESCE(KayitZamani, GETDATE()) AS date);
');");

            // PlakaNormalized: PlakaHelper.Normalize ile aynı form
            // (boşluk, tire, nokta, tab silinir; büyük harf).
            // Türkçe collation'da UPPER('i') = 'İ' olacağından Latin1 kullanılır.
            migrationBuilder.Sql(@"
EXEC('
UPDATE KantarGuvenlik
SET PlakaNormalized = NULLIF(
    UPPER(
        REPLACE(REPLACE(REPLACE(REPLACE(Plaka, '' '', ''''), ''-'', ''''), ''.'', ''''), CHAR(9), '''')
        COLLATE Latin1_General_100_CI_AS),
    '''')
WHERE Plaka IS NOT NULL;
');");

            migrationBuilder.CreateIndex(
                name: "IX_KantarGuvenlik_KayitZamani",
                table: "KantarGuvenlik",
                column: "KayitZamani");

            migrationBuilder.CreateIndex(
                name: "IX_KantarGuvenlik_PlakaNormalized",
                table: "KantarGuvenlik",
                column: "PlakaNormalized");

            migrationBuilder.CreateIndex(
                name: "IX_KantarGuvenlik_ZiyaretciKayitId",
                table: "KantarGuvenlik",
                column: "ZiyaretciKayitId",
                unique: true,
                filter: "[ZiyaretciKayitId] IS NOT NULL AND [Silindi] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_KantarGuvenlik_KayitZamani",
                table: "KantarGuvenlik");

            migrationBuilder.DropIndex(
                name: "IX_KantarGuvenlik_PlakaNormalized",
                table: "KantarGuvenlik");

            migrationBuilder.DropIndex(
                name: "IX_KantarGuvenlik_ZiyaretciKayitId",
                table: "KantarGuvenlik");

            migrationBuilder.DropColumn(
                name: "Boceklenme",
                table: "KantarKalite");

            migrationBuilder.DropColumn(
                name: "Curuk",
                table: "KantarKalite");

            migrationBuilder.DropColumn(
                name: "Koku",
                table: "KantarKalite");

            migrationBuilder.DropColumn(
                name: "Kuf",
                table: "KantarKalite");

            migrationBuilder.DropColumn(
                name: "Sonuc",
                table: "KantarKalite");

            migrationBuilder.DropColumn(
                name: "SonucAciklama",
                table: "KantarKalite");

            migrationBuilder.DropColumn(
                name: "Tozluluk",
                table: "KantarKalite");

            migrationBuilder.DropColumn(
                name: "BosaltimYapanUserId",
                table: "KantarGuvenlik");

            migrationBuilder.DropColumn(
                name: "BosaltimZamani",
                table: "KantarGuvenlik");

            migrationBuilder.DropColumn(
                name: "DokulduguYer",
                table: "KantarGuvenlik");

            migrationBuilder.DropColumn(
                name: "IrsaliyeNo",
                table: "KantarGuvenlik");

            migrationBuilder.DropColumn(
                name: "KaliteSonucu",
                table: "KantarGuvenlik");

            migrationBuilder.DropColumn(
                name: "PlakaNormalized",
                table: "KantarGuvenlik");

            migrationBuilder.DropColumn(
                name: "SonUrun",
                table: "KantarGuvenlik");

            migrationBuilder.DropColumn(
                name: "Tarih",
                table: "KantarGuvenlik");

            migrationBuilder.DropColumn(
                name: "UrunAdi",
                table: "KantarGuvenlik");

            migrationBuilder.DropColumn(
                name: "ZiyaretciKayitId",
                table: "KantarGuvenlik");
        }
    }
}
