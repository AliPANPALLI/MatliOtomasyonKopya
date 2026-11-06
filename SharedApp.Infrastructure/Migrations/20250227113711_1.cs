using Authentication.Domain.Models;
using Microsoft.EntityFrameworkCore.Migrations;
using SharedApp.Domain.Models;

#nullable disable

namespace SharedApp.Infrastructure.Migrations
{
    public partial class _1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"CREATE VIEW vwUserMakineYetki AS  SELECT dbo.UserMakineYetki.Id, dbo.UserMakineYetki.UserId, dbo.UserMakineYetki.MakineId, dbo.UserMakineYetki.BolumId, dbo.UserMakineYetki.LokasyonId, dbo.UserMakineYetki.SirketId, dbo.UserMakineYetki.[Read],
                  dbo.UserMakineYetki.Write, dbo.UserMakineYetki.Silindi, dbo.UserMakineYetki.SilinmeZamani, dbo.UserMakineYetki.Aktif, dbo.UserMakineYetki.SorumluMu, OTM_Auth.dbo.[User].Name, OTM_Auth.dbo.[User].Silindi AS UserSilindi,
                  OTM_Auth.dbo.[User].Email, OTM_Auth.dbo.[User].Surname, OTM_Auth.dbo.[User].PhoneNumber, OTM_Auth.dbo.[User].UserName
                  FROM     dbo.UserMakineYetki LEFT OUTER JOIN
                  OTM_Auth.dbo.[User] ON dbo.UserMakineYetki.UserId = OTM_Auth.dbo.[User].Id");

            migrationBuilder.Sql(@"CREATE VIEW vwUserKumesYetki AS SELECT OTM_Auth.dbo.[User].Name, OTM_Auth.dbo.[User].Silindi AS UserSilindi, OTM_Auth.dbo.[User].Email, OTM_Auth.dbo.[User].Surname, OTM_Auth.dbo.[User].PhoneNumber, OTM_Auth.dbo.[User].UserName, dbo.UserKumesYetki.*
                 FROM dbo.UserKumesYetki LEFT OUTER JOIN
                 OTM_Auth.dbo.[User] ON dbo.UserKumesYetki.UserId = OTM_Auth.dbo.[User].Id");

            migrationBuilder.Sql(@"CREATE VIEW vwUserSuKaynakYetki AS SELECT OTM_Auth.dbo.[User].Name, OTM_Auth.dbo.[User].Silindi AS UserSilindi, OTM_Auth.dbo.[User].Email, OTM_Auth.dbo.[User].Surname, OTM_Auth.dbo.[User].PhoneNumber, OTM_Auth.dbo.[User].UserName, dbo.UserSuKaynakYetki.*
                 FROM     OTM_Auth.dbo.[User] RIGHT OUTER JOIN
                 dbo.UserSuKaynakYetki ON OTM_Auth.dbo.[User].Id = dbo.UserSuKaynakYetki.UserId");
        }
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
