using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Authentication.Infrastructure.Migrations
{
    public partial class _4 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"CREATE VIEW vwConnectSiteLog AS SELECT  dbo.[User].Name, dbo.[User].Surname, dbo.[User].Email, dbo.[User].PhoneNumber, dbo.[User].UserName, dbo.[User].ProfilePhoto, dbo.[User].Position, dbo.[User].Silindi, OTM_Shared.dbo.Lokasyon.Name AS LokasyonName, 
                         OTM_Shared.dbo.Sirket.Name AS SirketName, dbo.ConnectSiteLog.UserId, { fn CONCAT({ fn CONCAT(dbo.[User].Name, ' ') }, dbo.[User].Surname) } AS UserFullName, dbo.ConnectSiteLog.LocalName, 
                         dbo.ConnectSiteLog.HostName, dbo.ConnectSiteLog.Browser, dbo.ConnectSiteLog.BrowserVersion, dbo.ConnectSiteLog.IsMobile, dbo.ConnectSiteLog.NetIp, dbo.ConnectSiteLog.LocalIp, dbo.ConnectSiteLog.Tarih, 
                         dbo.ConnectSiteLog.Aciklama, dbo.ConnectSiteLog.Id
                         FROM            dbo.ConnectSiteLog LEFT OUTER JOIN
                         dbo.[User] ON dbo.ConnectSiteLog.UserId = dbo.[User].Id LEFT OUTER JOIN
                         OTM_Shared.dbo.Lokasyon ON dbo.[User].LokasyonId = OTM_Shared.dbo.Lokasyon.Id LEFT OUTER JOIN
                         OTM_Shared.dbo.Sirket ON OTM_Shared.dbo.Lokasyon.SirketId = OTM_Shared.dbo.Sirket.Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
