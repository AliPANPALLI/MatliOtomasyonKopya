using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KumesScada.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUygulamaTipColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Tip",
                table: "Uygulama",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Tip",
                table: "Uygulama");
        }
    }
}
