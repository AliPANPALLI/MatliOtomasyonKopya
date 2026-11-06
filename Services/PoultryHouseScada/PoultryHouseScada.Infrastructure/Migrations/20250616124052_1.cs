using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PoultryHouseScada.Infrastructure.Migrations
{
    public partial class _1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CizimKumesEsleme",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CizimId = table.Column<int>(type: "int", nullable: true),
                    KumesId = table.Column<int>(type: "int", nullable: true),
                    SekilId = table.Column<int>(type: "int", nullable: true),
                    Silindi = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CizimKumesEsleme", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CizimKumesEsleme");
        }
    }
}
