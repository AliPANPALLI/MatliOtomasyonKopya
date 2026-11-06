using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Authentication.Infrastructure.Migrations
{
    public partial class _9 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"CREATE VIEW vwPageVisit AS WITH cteLastSuccessfulLogin AS (
    SELECT *
    FROM (
        SELECT 
            csl.*, 
            ROW_NUMBER() OVER (PARTITION BY csl.UserId ORDER BY csl.Tarih DESC) AS rn
        FROM dbo.vwConnectSiteLog csl
        WHERE csl.Aciklama = 'Başarılı Giriş'
    ) AS loginRanked
    WHERE rn = 1
)

SELECT 
    u.Id AS UserId,
    u.Name,
    u.Surname,
    { fn CONCAT({ fn CONCAT(u.Name, ' ') }, u.Surname) } AS UserFullName,
    u.Email,
    u.PhoneNumber,
    u.UserName,
    u.ProfilePhoto,
    u.Position,
    u.Silindi,
    s.Name AS SirketName,
    l.Name AS LokasyonName,
    
    ll.LocalName,
    ll.HostName,
    ll.Browser,
    ll.BrowserVersion,
    ll.IsMobile,
    ll.NetIp,
    ll.LocalIp,
    ll.Tarih AS LoginTime,
    ll.Aciklama,
    
    pv.PageName,
    pv.ZiyaretZamani,
    pv.FormLink,
    pv.ModulLink,
    pv.UrlId,
    
    m.Name AS ModulName,
    f.Name AS FormName,

    CASE 
        WHEN pv.FormLink = '/kumes-scada' THEN ks.Name
        WHEN pv.FormLink IN ('/tesis-genel', '/su-hat-izleme') THEN l2.Name
        ELSE NULL
    END AS UrlTargetIdName

FROM cteLastSuccessfulLogin ll
LEFT JOIN dbo.[User] u ON ll.UserId = u.Id
LEFT JOIN OTM_Shared.dbo.Lokasyon l ON u.LokasyonId = l.Id
LEFT JOIN OTM_Shared.dbo.Sirket s ON l.SirketId = s.Id
LEFT JOIN dbo.PageVisit pv ON 
    pv.UserId = ll.UserId 
    AND pv.ZiyaretZamani >= ll.Tarih
    AND pv.CikisZamani IS NULL  -- 🔍 sadece açık sayfalar
LEFT JOIN OTM_Shared.dbo.Modul m ON m.Link = pv.ModulLink
LEFT JOIN OTM_Shared.dbo.Form f ON f.Link = pv.FormLink
LEFT JOIN OTM_KumesScada.dbo.Kumes ks ON pv.FormLink = '/kumes-scada' AND pv.UrlId = ks.Id
LEFT JOIN OTM_Shared.dbo.Lokasyon l2 ON pv.UrlId = l2.Id AND pv.FormLink IN ('/tesis-genel', '/su-hat-izleme')
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
