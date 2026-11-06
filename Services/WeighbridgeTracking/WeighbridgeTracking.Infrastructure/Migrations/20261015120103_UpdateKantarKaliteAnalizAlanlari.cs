using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeighbridgeTracking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateKantarKaliteAnalizAlanlari : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "KaliteVeri1",
                table: "KantarKalite");

            migrationBuilder.DropColumn(
                name: "KaliteVeri2",
                table: "KantarKalite");

            migrationBuilder.AddColumn<decimal>(
                name: "Koh",
                table: "KantarKalite",
                type: "decimal(10,3)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Kul",
                table: "KantarKalite",
                type: "decimal(10,3)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Nem",
                table: "KantarKalite",
                type: "decimal(10,3)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Nisasta",
                table: "KantarKalite",
                type: "decimal(10,3)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Protein",
                table: "KantarKalite",
                type: "decimal(10,3)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Seluloz",
                table: "KantarKalite",
                type: "decimal(10,3)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Ureaz",
                table: "KantarKalite",
                type: "decimal(10,3)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Yag",
                table: "KantarKalite",
                type: "decimal(10,3)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Koh",
                table: "KantarKalite");

            migrationBuilder.DropColumn(
                name: "Kul",
                table: "KantarKalite");

            migrationBuilder.DropColumn(
                name: "Nem",
                table: "KantarKalite");

            migrationBuilder.DropColumn(
                name: "Nisasta",
                table: "KantarKalite");

            migrationBuilder.DropColumn(
                name: "Protein",
                table: "KantarKalite");

            migrationBuilder.DropColumn(
                name: "Seluloz",
                table: "KantarKalite");

            migrationBuilder.DropColumn(
                name: "Ureaz",
                table: "KantarKalite");

            migrationBuilder.DropColumn(
                name: "Yag",
                table: "KantarKalite");

            migrationBuilder.AddColumn<string>(
                name: "KaliteVeri1",
                table: "KantarKalite",
                type: "varchar(100)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KaliteVeri2",
                table: "KantarKalite",
                type: "varchar(100)",
                nullable: true);
        }
    }
}
