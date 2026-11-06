using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SharedApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class _39 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SeviyeNo",
                table: "GonderilenSms",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SonDenemeZamani",
                table: "GonderilenSms",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "GonderilenSms",
                type: "nvarchar(450)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SeviyeNo",
                table: "GonderilenSms");

            migrationBuilder.DropColumn(
                name: "SonDenemeZamani",
                table: "GonderilenSms");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "GonderilenSms");
        }
    }
}
