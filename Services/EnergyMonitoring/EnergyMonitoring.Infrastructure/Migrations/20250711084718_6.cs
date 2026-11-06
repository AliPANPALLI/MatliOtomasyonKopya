using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnergyMonitoring.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class _6 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CihazModel",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Marka = table.Column<string>(type: "varchar(100)", nullable: false),
                    Model = table.Column<string>(type: "varchar(100)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CihazModel", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Makine_BolumId",
                table: "Makine",
                column: "BolumId");

            migrationBuilder.CreateIndex(
                name: "IX_Cihaz_GatewayId",
                table: "Cihaz",
                column: "GatewayId");

            migrationBuilder.CreateIndex(
                name: "IX_Cihaz_MakineId",
                table: "Cihaz",
                column: "MakineId");

            migrationBuilder.CreateIndex(
                name: "IX_Cihaz_ModelId",
                table: "Cihaz",
                column: "ModelId");

            migrationBuilder.AddForeignKey(
                name: "FK_Cihaz_CihazModel_ModelId",
                table: "Cihaz",
                column: "ModelId",
                principalTable: "CihazModel",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Cihaz_Gateways_GatewayId",
                table: "Cihaz",
                column: "GatewayId",
                principalTable: "Gateways",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Cihaz_Makine_MakineId",
                table: "Cihaz",
                column: "MakineId",
                principalTable: "Makine",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Makine_Bolum_BolumId",
                table: "Makine",
                column: "BolumId",
                principalTable: "Bolum",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cihaz_CihazModel_ModelId",
                table: "Cihaz");

            migrationBuilder.DropForeignKey(
                name: "FK_Cihaz_Gateways_GatewayId",
                table: "Cihaz");

            migrationBuilder.DropForeignKey(
                name: "FK_Cihaz_Makine_MakineId",
                table: "Cihaz");

            migrationBuilder.DropForeignKey(
                name: "FK_Makine_Bolum_BolumId",
                table: "Makine");

            migrationBuilder.DropTable(
                name: "CihazModel");

            migrationBuilder.DropIndex(
                name: "IX_Makine_BolumId",
                table: "Makine");

            migrationBuilder.DropIndex(
                name: "IX_Cihaz_GatewayId",
                table: "Cihaz");

            migrationBuilder.DropIndex(
                name: "IX_Cihaz_MakineId",
                table: "Cihaz");

            migrationBuilder.DropIndex(
                name: "IX_Cihaz_ModelId",
                table: "Cihaz");
        }
    }
}
