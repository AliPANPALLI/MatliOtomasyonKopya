using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PanelIdentification.Infrastructure.Migrations
{
    public partial class _1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"CREATE VIEW vwOnayIslemKayit AS  SELECT DISTINCT 
                  oik.UserId, oik.OgeId, oik.OgeTipi, CASE WHEN oik.OgeTipi = 'pano' THEN p.Name WHEN oik.OgeTipi = 'bolum' THEN b.Name WHEN oik.OgeTipi = 'makine' THEN m.Name WHEN oik.OgeTipi = 'cihaz' THEN c.Name ELSE NULL 
                  END AS OgeName, { fn CONCAT({ fn CONCAT(OTM_Auth.dbo.[User].Name, ' ') }, OTM_Auth.dbo.[User].Surname) } AS UserFullName
                  FROM     dbo.OnayIslemKayit AS oik LEFT OUTER JOIN
                  dbo.Pano AS p ON oik.OgeTipi = 'pano' AND oik.OgeId = p.Id LEFT OUTER JOIN
                  dbo.Bolum AS b ON oik.OgeTipi = 'bolum' AND oik.OgeId = b.Id LEFT OUTER JOIN
                  dbo.Makine AS m ON oik.OgeTipi = 'makine' AND oik.OgeId = m.Id LEFT OUTER JOIN
                  dbo.Cihaz AS c ON oik.OgeTipi = 'cihaz' AND oik.OgeId = c.Id LEFT OUTER JOIN
                  OTM_Auth.dbo.[User] ON oik.UserId = OTM_Auth.dbo.[User].Id
                  WHERE  (oik.Durum = 0)");
    
            migrationBuilder.AddColumn<bool>(
                name: "OnayaGonderildiMi",
                table: "Pano",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LokasyonId",
                table: "OnayIslemKayit",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "OnayaGonderildiMi",
                table: "Makine",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "OnayaGonderildiMi",
                table: "Cihaz",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "OnayaGonderildiMi",
                table: "Bolum",
                type: "bit",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OnayaGonderildiMi",
                table: "Pano");

            migrationBuilder.DropColumn(
                name: "LokasyonId",
                table: "OnayIslemKayit");

            migrationBuilder.DropColumn(
                name: "OnayaGonderildiMi",
                table: "Makine");

            migrationBuilder.DropColumn(
                name: "OnayaGonderildiMi",
                table: "Cihaz");

            migrationBuilder.DropColumn(
                name: "OnayaGonderildiMi",
                table: "Bolum");
        }
    }
}
