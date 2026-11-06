using Microsoft.EntityFrameworkCore.Migrations;
using System.Security.Cryptography;

#nullable disable

namespace PanoKimliklendirme.Infrastructure.Migrations
{
    public partial class _13 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER VIEW vwDistinctOnayIslemKayit AS SELECT DISTINCT
                  oik.UserId, oik.OgeId, oik.OgeTipi, CASE WHEN oik.OgeTipi = 'pano' THEN p.Name WHEN oik.OgeTipi = 'bolum' THEN b.Name WHEN oik.OgeTipi = 'makine' THEN m.Name WHEN oik.OgeTipi = 'cihaz' THEN c.Name ELSE NULL
                  END AS OgeName, { fn CONCAT({ fn CONCAT(OTM_Auth.dbo.[User].Name, ' ') }, OTM_Auth.dbo.[User].Surname) }
                  AS UserFullName, oik.LokasyonId, oik.PanoId, ls.Name AS LokasyonName, sirket.Name AS SirketName, oik.DosyaAd
                  FROM     dbo.OnayIslemKayit AS oik LEFT OUTER JOIN
                  dbo.Pano AS p ON oik.OgeTipi = 'pano' AND oik.OgeId = p.Id LEFT OUTER JOIN
                  dbo.Bolum AS b ON oik.OgeTipi = 'bolum' AND oik.OgeId = b.Id LEFT OUTER JOIN
                  dbo.Makine AS m ON oik.OgeTipi = 'makine' AND oik.OgeId = m.Id LEFT OUTER JOIN
                  dbo.Cihaz AS c ON oik.OgeTipi = 'cihaz' AND oik.OgeId = c.Id LEFT OUTER JOIN
                  OTM_Auth.dbo.[User] ON oik.UserId = OTM_Auth.dbo.[User].Id LEFT OUTER JOIN
                  OTM_Shared.dbo.Lokasyon AS ls ON oik.LokasyonId = ls.Id LEFT OUTER JOIN
                  OTM_Shared.dbo.Sirket AS sirket ON ls.SirketId = sirket.Id
                  WHERE(oik.Durum = 0) AND(oik.OnayaGonderildiMi = 1)"); 
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
