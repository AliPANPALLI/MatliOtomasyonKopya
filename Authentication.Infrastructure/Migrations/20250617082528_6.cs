using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Authentication.Infrastructure.Migrations
{
    public partial class _6 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"CREATE VIEW vwPageVisit AS SELECT        pv.Id, pv.UserId, pv.PageName, pv.ZiyaretZamani, pv.FormLink, pv.ModulLink, pv.UrlId, u.UserName, { fn CONCAT({ fn CONCAT(u.Name, ' ') }, u.Surname) } AS UserFullName, m.Name AS ModulName, 
                         CASE WHEN pv.FormLink = '/kumes-scada' THEN k.Name WHEN pv.FormLink IN ('/tesis-genel', '/su-hat-izleme') THEN l2.Name ELSE NULL END AS UrlTargetName, f.Name
                         FROM            dbo.PageVisit AS pv LEFT OUTER JOIN
                         dbo.[User] AS u ON pv.UserId = u.Id LEFT OUTER JOIN
                         OTM_Shared.dbo.Lokasyon AS l ON u.LokasyonId = l.Id LEFT OUTER JOIN
                         OTM_Shared.dbo.Modul AS m ON m.Link = pv.ModulLink LEFT OUTER JOIN
                         OTM_Shared.dbo.Form AS f ON f.Link = pv.FormLink LEFT OUTER JOIN
                         OTM_KumesScada.dbo.Kumes AS k ON pv.FormLink = '/kumes-scada' AND pv.UrlId = k.Id LEFT OUTER JOIN
                         OTM_Shared.dbo.Lokasyon AS l2 ON pv.UrlId = l2.Id AND pv.FormLink IN ('/tesis-genel', '/su-hat-izleme')");
            migrationBuilder.AddColumn<string>(
                name: "FormLink",
                table: "PageVisit",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModulLink",
                table: "PageVisit",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UrlId",
                table: "PageVisit",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FormLink",
                table: "PageVisit");

            migrationBuilder.DropColumn(
                name: "ModulLink",
                table: "PageVisit");

            migrationBuilder.DropColumn(
                name: "UrlId",
                table: "PageVisit");
        }
    }
}
