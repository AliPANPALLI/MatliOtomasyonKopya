using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeighbridgeTracking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class KaliteKontrolPersonel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "KaliteKontrolPersonelId",
                table: "KantarKalite",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "KaliteKontrolPersonel",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AdSoyad = table.Column<string>(type: "nvarchar(200)", nullable: true),
                    Silindi = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KaliteKontrolPersonel", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KantarKalite_KaliteKontrolPersonelId",
                table: "KantarKalite",
                column: "KaliteKontrolPersonelId");

            migrationBuilder.AddForeignKey(
                name: "FK_KantarKalite_KaliteKontrolPersonel_KaliteKontrolPersonelId",
                table: "KantarKalite",
                column: "KaliteKontrolPersonelId",
                principalTable: "KaliteKontrolPersonel",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_KantarKalite_KaliteKontrolPersonel_KaliteKontrolPersonelId",
                table: "KantarKalite");

            migrationBuilder.DropTable(
                name: "KaliteKontrolPersonel");

            migrationBuilder.DropIndex(
                name: "IX_KantarKalite_KaliteKontrolPersonelId",
                table: "KantarKalite");

            migrationBuilder.DropColumn(
                name: "KaliteKontrolPersonelId",
                table: "KantarKalite");
        }
    }
}
