using ProjectTracking.API.Middleware;
using ProjectTracking.Infrastructure;
using ProjectTracking.Infrastructure.Persistence;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using ProjectTracking.API.Security;
using MatliOtomasyon.Shared.Infrastructure.Configuration;
using MatliOtomasyon.Shared.Web.Security;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentPlaceholders();

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});
builder.Services.AddProjeTakipInfrastructure(builder.Configuration);
builder.Services.Configure<CustomTokenOption>(builder.Configuration.GetSection("TokenOption"));
builder.Services.AddCustomTokenAuth(builder.Configuration.GetSection("TokenOption").Get<CustomTokenOption>()
    ?? throw new InvalidOperationException("TokenOption yapılandırması bulunamadı."));
builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ProjeTakipAccessService>();
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options => options.AddPolicy("Portal", policy => policy
    .WithOrigins(
        "http://localhost:4200",
        "https://localhost:4200",
        "http://localhost:4201",
        "https://localhost:4201",
        "http://localhost:4202",
        "https://localhost:4202",
        "https://otomasyon.matli.com.tr",
        "https://otomasyontest.matli.com.tr")
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var database = scope.ServiceProvider.GetRequiredService<ProjeTakipDbContext>();
    // Tek round-trip: aşağıdaki 6 idempotent şema kontrolü tek batch içinde çalışır.
    // Her modül ilk açılışta bu bloğu (soğuk başlangıçta) çalıştırdığından, ayrı ayrı
    // gönderilen 6 sorgu round-trip gecikmesini artırıyordu; artık tek istek yeterli.
    await database.Database.ExecuteSqlRawAsync("""
        IF OBJECT_ID('dbo.Sirket', 'U') IS NULL
        BEGIN
            EXEC(N'CREATE TABLE dbo.Sirket
            (
                Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Sirket PRIMARY KEY,
                Ad nvarchar(150) NOT NULL,
                Aktif bit NOT NULL CONSTRAINT DF_Sirket_Aktif DEFAULT CAST(1 AS bit),
                OlusturulmaUtc datetime2 NOT NULL CONSTRAINT DF_Sirket_OlusturulmaUtc DEFAULT SYSUTCDATETIME(),
                GuncellemeUtc datetime2 NULL CONSTRAINT DF_Sirket_GuncellemeUtc DEFAULT SYSUTCDATETIME(),
                RowVersion rowversion NOT NULL,
                CONSTRAINT UQ_Sirket_Ad UNIQUE (Ad)
            )');
        END;

        IF COL_LENGTH('dbo.Tesis', 'SirketId') IS NULL
        BEGIN
            IF NOT EXISTS (SELECT 1 FROM dbo.Sirket WHERE Ad = N'Belirtilmedi')
                INSERT INTO dbo.Sirket (Ad, Aktif) VALUES (N'Belirtilmedi', 1);

            DECLARE @VarsayilanSirketId int = (SELECT TOP (1) Id FROM dbo.Sirket WHERE Ad = N'Belirtilmedi');
            EXEC(N'ALTER TABLE dbo.Tesis ADD SirketId int NULL');
            EXEC sp_executesql
                N'UPDATE dbo.Tesis SET SirketId = @SirketId WHERE SirketId IS NULL',
                N'@SirketId int',
                @SirketId = @VarsayilanSirketId;
            EXEC(N'ALTER TABLE dbo.Tesis ALTER COLUMN SirketId int NOT NULL');
            EXEC(N'ALTER TABLE dbo.Tesis ADD CONSTRAINT FK_Tesis_Sirket FOREIGN KEY (SirketId) REFERENCES dbo.Sirket(Id)');
            EXEC(N'CREATE INDEX IX_Tesis_SirketId ON dbo.Tesis (SirketId)');
        END;

        IF COL_LENGTH('dbo.Tesis', 'IlKodu') IS NULL
        BEGIN
            EXEC('ALTER TABLE dbo.Tesis ADD IlKodu int NULL');
            EXEC('ALTER TABLE dbo.Tesis ADD CONSTRAINT CK_Tesis_IlKodu CHECK (IlKodu IS NULL OR IlKodu BETWEEN 1 AND 81)');
            EXEC('CREATE INDEX IX_Tesis_IlKodu ON dbo.Tesis (IlKodu)');
        END;

        IF OBJECT_ID('dbo.IcmalKalemi', 'U') IS NULL
        BEGIN
            CREATE TABLE dbo.IcmalKalemi
            (
                Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_IcmalKalemi PRIMARY KEY,
                ProjeDokumaniId int NOT NULL,
                IsAdimiId int NULL,
                SiraNo int NOT NULL,
                PozNo nvarchar(100) NOT NULL CONSTRAINT DF_IcmalKalemi_PozNo DEFAULT N'',
                Tanim nvarchar(500) NOT NULL,
                Birim nvarchar(20) NOT NULL,
                Miktar decimal(18,3) NOT NULL,
                BirimFiyat decimal(18,4) NOT NULL,
                ParaBirimi char(3) NOT NULL,
                Tutar decimal(18,2) NOT NULL,
                Aciklama nvarchar(500) NOT NULL CONSTRAINT DF_IcmalKalemi_Aciklama DEFAULT N'',
                OlusturulmaUtc datetime2 NOT NULL CONSTRAINT DF_IcmalKalemi_OlusturulmaUtc DEFAULT SYSUTCDATETIME(),
                GuncellemeUtc datetime2 NULL,
                RowVersion rowversion NOT NULL,
                CONSTRAINT FK_IcmalKalemi_ProjeDokumani FOREIGN KEY (ProjeDokumaniId)
                    REFERENCES dbo.ProjeDokumani(Id) ON DELETE CASCADE,
                CONSTRAINT FK_IcmalKalemi_IsAdimi FOREIGN KEY (IsAdimiId)
                    REFERENCES dbo.IsAdimi(Id),
                CONSTRAINT CK_IcmalKalemi_SiraNo CHECK (SiraNo > 0),
                CONSTRAINT CK_IcmalKalemi_Miktar CHECK (Miktar > 0),
                CONSTRAINT CK_IcmalKalemi_BirimFiyat CHECK (BirimFiyat >= 0),
                CONSTRAINT CK_IcmalKalemi_ParaBirimi CHECK (ParaBirimi IN ('TRY', 'USD', 'EUR', 'GBP'))
            );
            CREATE INDEX IX_IcmalKalemi_ProjeDokumaniId_SiraNo ON dbo.IcmalKalemi (ProjeDokumaniId, SiraNo);
            CREATE INDEX IX_IcmalKalemi_IsAdimiId ON dbo.IcmalKalemi (IsAdimiId);
        END;

        IF OBJECT_ID('dbo.ProjeTakipYetkisi', 'U') IS NULL
        BEGIN
            CREATE TABLE dbo.ProjeTakipYetkisi
            (
                Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_ProjeTakipYetkisi PRIMARY KEY,
                KullaniciId nvarchar(128) NOT NULL,
                KullaniciAdi nvarchar(200) NOT NULL CONSTRAINT DF_ProjeTakipYetkisi_KullaniciAdi DEFAULT N'',
                Goruntule bit NOT NULL CONSTRAINT DF_ProjeTakipYetkisi_Goruntule DEFAULT CAST(0 AS bit),
                Duzenle bit NOT NULL CONSTRAINT DF_ProjeTakipYetkisi_Duzenle DEFAULT CAST(0 AS bit),
                MaliyetIcmalGor bit NOT NULL CONSTRAINT DF_ProjeTakipYetkisi_MaliyetIcmalGor DEFAULT CAST(0 AS bit),
                IcmalDuzenle bit NOT NULL CONSTRAINT DF_ProjeTakipYetkisi_IcmalDuzenle DEFAULT CAST(0 AS bit),
                SilArsivle bit NOT NULL CONSTRAINT DF_ProjeTakipYetkisi_SilArsivle DEFAULT CAST(0 AS bit),
                YetkiYonet bit NOT NULL CONSTRAINT DF_ProjeTakipYetkisi_YetkiYonet DEFAULT CAST(0 AS bit),
                OlusturulmaUtc datetime2 NOT NULL CONSTRAINT DF_ProjeTakipYetkisi_OlusturulmaUtc DEFAULT SYSUTCDATETIME(),
                GuncellemeUtc datetime2 NULL,
                RowVersion rowversion NOT NULL,
                CONSTRAINT UQ_ProjeTakipYetkisi_KullaniciId UNIQUE (KullaniciId)
            );
        END;

        IF OBJECT_ID('dbo.ProjeTakipDenetimKaydi', 'U') IS NULL
        BEGIN
            CREATE TABLE dbo.ProjeTakipDenetimKaydi
            (
                Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_ProjeTakipDenetimKaydi PRIMARY KEY,
                KullaniciId nvarchar(128) NOT NULL,
                KullaniciAdi nvarchar(200) NOT NULL,
                Islem nvarchar(50) NOT NULL,
                KayitTuru nvarchar(80) NOT NULL,
                KayitId nvarchar(80) NOT NULL,
                Aciklama nvarchar(1000) NOT NULL,
                IpAdresi nvarchar(64) NOT NULL,
                IslemUtc datetime2 NOT NULL CONSTRAINT DF_ProjeTakipDenetimKaydi_IslemUtc DEFAULT SYSUTCDATETIME()
            );
            CREATE INDEX IX_ProjeTakipDenetimKaydi_IslemUtc ON dbo.ProjeTakipDenetimKaydi (IslemUtc DESC);
            CREATE INDEX IX_ProjeTakipDenetimKaydi_Kayit ON dbo.ProjeTakipDenetimKaydi (KayitTuru, KayitId);
        END;
        """);
}

app.UseMiddleware<ApiExceptionMiddleware>();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseResponseCompression();
app.UseHttpsRedirection();
app.UseCors("Portal");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

public partial class Program;
