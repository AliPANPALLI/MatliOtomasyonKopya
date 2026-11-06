using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Authentication.Infrastructure.Migrations
{
    public partial class _12 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "CikisZamani",
                table: "PageVisit",
                type: "datetime",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "PageVisitMain",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    PageName = table.Column<string>(type: "varchar(300)", nullable: true),
                    ModulLink = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FormLink = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UrlId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ZiyaretZamani = table.Column<DateTime>(type: "datetime", nullable: true),
                    CikisZamani = table.Column<DateTime>(type: "datetime", nullable: true),
                    TabId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PageVisitMain", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserHeartBeatMain",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LastSeen = table.Column<DateTime>(type: "datetime", nullable: true),
                    TabId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserHeartBeatMain", x => x.UserId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PageVisitMain");

            migrationBuilder.DropTable(
                name: "UserHeartBeatMain");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CikisZamani",
                table: "PageVisit",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime",
                oldNullable: true);
        }
    }
}
