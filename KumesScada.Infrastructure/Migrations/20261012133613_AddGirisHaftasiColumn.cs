using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KumesScada.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGirisHaftasiColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GirisHaftasi",
                table: "YumurtlamaDonemleri",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GirisHaftasi",
                table: "YumurtlamaDonemleri");
        }
    }
}
