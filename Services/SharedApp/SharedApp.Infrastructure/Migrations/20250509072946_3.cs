using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SharedApp.Infrastructure.Migrations
{
    public partial class _3 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AlarmGruplar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AlarmGrupAd = table.Column<string>(type: "varchar(50)", nullable: true),
                    Aciklama = table.Column<string>(type: "varchar(150)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlarmGruplar", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AlarmKullaniciEsleme",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    AlarmId = table.Column<int>(type: "int", nullable: true),
                    AlarmGrupId = table.Column<int>(type: "int", nullable: true),
                    Silindi = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlarmKullaniciEsleme", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Alarmlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AlarmAd = table.Column<string>(type: "varchar(100)", nullable: true),
                    AlarmMetni = table.Column<string>(type: "varchar(300)", nullable: true),
                    AlarmGrubu = table.Column<int>(type: "int", nullable: true),
                    Aciklama = table.Column<string>(type: "varchar(150)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alarmlar", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlarmGruplar");

            migrationBuilder.DropTable(
                name: "AlarmKullaniciEsleme");

            migrationBuilder.DropTable(
                name: "Alarmlar");
        }
    }
}
