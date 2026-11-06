using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SharedApp.Infrastructure.Migrations
{
    public partial class _2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MailGonderimTakip",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KayitZamani = table.Column<DateTime>(type: "datetime", nullable: true),
                    MailBasligi = table.Column<string>(type: "varchar(150)", nullable: true),
                    MailIcerik = table.Column<string>(type: "varchar(MAX)", nullable: true),
                    EkYolu = table.Column<string>(type: "varchar(350)", nullable: true),
                    GonderimBasarili = table.Column<bool>(type: "bit", nullable: true),
                    HataMesaji = table.Column<string>(type: "varchar(500)", nullable: true),
                    GonderilecekTo = table.Column<string>(type: "varchar(MAX)", nullable: true),
                    GonderilecekCc = table.Column<string>(type: "varchar(MAX)", nullable: true),
                    SonDenemeZamani = table.Column<DateTime>(type: "datetime", nullable: true),
                    KalanDenemeSayisi = table.Column<short>(type: "smallint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MailGonderimTakip", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MailGonderimTakip");
        }
    }
}
