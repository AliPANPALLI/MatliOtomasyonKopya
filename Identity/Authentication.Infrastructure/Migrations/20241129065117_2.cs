using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Authentication.Infrastructure.Migrations
{
    public partial class _2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"CREATE VIEW vwUser AS SELECT dbo.[User].Id, dbo.[User].Name, dbo.[User].Surname, dbo.[User].Email, dbo.[User].PhoneNumber, dbo.[User].UserName, dbo.[User].IsLocked, dbo.[User].LokasyonId, dbo.[User].ProfilePhoto, dbo.[User].Position, dbo.[User].RequiresOtp, 
                  dbo.[User].Silindi, OTM_Shared.dbo.Sirket.Id AS SirketId, OTM_Shared.dbo.Lokasyon.Name AS LokasyonName, OTM_Shared.dbo.Sirket.Name AS SirketName
FROM     OTM_Shared.dbo.Lokasyon RIGHT OUTER JOIN
                  dbo.[User] ON OTM_Shared.dbo.Lokasyon.Id = dbo.[User].LokasyonId LEFT OUTER JOIN
                  OTM_Shared.dbo.Sirket ON OTM_Shared.dbo.Lokasyon.SirketId = OTM_Shared.dbo.Sirket.Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
