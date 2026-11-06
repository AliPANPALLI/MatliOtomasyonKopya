using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeighbridgeTracking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class _2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "KantarGuvenlikId",
                table: "KantarKalite",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_KantarKalite_KantarGuvenlikId",
                table: "KantarKalite",
                column: "KantarGuvenlikId");

            migrationBuilder.AddForeignKey(
                name: "FK_KantarKalite_KantarGuvenlik_KantarGuvenlikId",
                table: "KantarKalite",
                column: "KantarGuvenlikId",
                principalTable: "KantarGuvenlik",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_KantarKalite_KantarGuvenlik_KantarGuvenlikId",
                table: "KantarKalite");

            migrationBuilder.DropIndex(
                name: "IX_KantarKalite_KantarGuvenlikId",
                table: "KantarKalite");

            migrationBuilder.DropColumn(
                name: "KantarGuvenlikId",
                table: "KantarKalite");
        }
    }
}
