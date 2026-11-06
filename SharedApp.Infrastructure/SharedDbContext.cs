using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SharedApp.Domain.DbViewModels;
using SharedApp.Domain.Models;
using SharedApp.Domain.Models.ReportModels;
using System.Reflection.Emit;

namespace SharedApp.Infrastructure
{
    public class SharedDbContext : DbContext
    {
        private IConfiguration _configuration;
        public SharedDbContext(DbContextOptions<DbContext> options)
     : base(options)
        {
        }

        public SharedDbContext(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(_configuration.GetConnectionString("SharedSqlServer"));
        }
        public DbSet<AlarmGrupAlarmEsleme> AlarmGrupAlarmEsleme { get; set; }
        public DbSet<AlarmKullaniciEsleme> AlarmKullaniciEsleme { get; set; }
        public DbSet<Alarmlar> Alarmlar { get; set; }
        public DbSet<AlarmGruplar> AlarmGruplar { get; set; }
        public DbSet<AlarmLog> AlarmLog { get; set; }
        public DbSet<Bolum> Bolum { get; set; }
        public DbSet<Form> Form { get; set; }
        public DbSet<FormLokasyonEsleme> FormLokasyonEsleme { get; set; }
        public DbSet<FormLinkEsleme> FormLinkEsleme { get; set; }
        public DbSet<GenelParametreler> GenelParametreler { get; set; }
        public DbSet<GonderilenSms> GonderilenSms { get; set; }
        public DbSet<Lokasyon> Lokasyon { get; set; }
        public DbSet<MailGonderimTakip> MailGonderimTakip { get; set; }
        public DbSet<MesajGonderimTakip> MesajGonderimTakip { get; set; }
        public DbSet<MesajGonderimTakipAlarmLog> MesajGonderimTakipAlarmLog { get; set; }
        public DbSet<Modul> Modul { get; set; }
        public DbSet<ServisKontrol> ServisKontrol { get; set; }
        public DbSet<Sirket> Sirket { get; set; }
        public DbSet<SonGelistirmeler> SonGelistirmeler { get; set; }
        public DbSet<Tesis> Tesis { get; set; }
        public DbSet<UserEnerjiIzlemeYetki> UserEnerjiIzlemeYetki { get; set; }
        public DbSet<UserFormYetki> UserFormYetki { get; set; }
        public DbSet<UserMakineYetki> UserMakineYetki { get; set; }
        public DbSet<UserKumesYetki> UserKumesYetki { get; set; }
        public DbSet<UserLinkYetki> UserLinkYetki { get; set; }
        public DbSet<UserYetki> UserYetki { get; set; }
        public DbSet<UserSuKaynakYetki> UserSuKaynakYetki { get; set; }
        public DbSet<vwAlarmKullaniciEsleme> vwAlarmKullaniciEsleme { get; set; }
        public DbSet<vwAlarmKullaniciEslemeForNotification> vwAlarmKullaniciEslemeForNotification { get; set; }
        public DbSet<vwAlarmlar> vwAlarmlar { get; set; }
        public DbSet<vwForm> vwForm { get; set; }
        public DbSet<vwUserFormYetki> vwUserFormYetki { get; set; }
        public DbSet<vwFormLokasyonEsleme> vwFormLokasyonEsleme { get; set; }
        public DbSet<vwUserMakineYetki> vwUserMakineYetki { get; set; }
        public DbSet<vwUserKumesYetki> vwUserKumesYetki { get; set; }
        public DbSet<vwUserLinkYetki> vwUserLinkYetki { get; set; }
        public DbSet<vwUserSuKaynakYetki> vwUserSuKaynakYetki { get; set; }
        public DbSet<vwSuHatCizimSirketForYetki> vwSuHatCizimSirketForYetki { get; set; }
        public DbSet<YanginAlgilamaLogs> YanginAlgilamaLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<AlarmKullaniciEsleme>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.UserId).HasColumnType("nvarchar(450)");
                entity.Property(e => e.SilinmeZamani).HasColumnType("datetime");
                entity.Property(e => e.BildirimYontemi).HasColumnType("varchar(15)");
            });

            builder.Entity<AlarmGrupAlarmEsleme>(entity =>
            {
                entity.HasKey(x => x.Id);
            });

            builder.Entity<AlarmGruplar>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.AlarmGrupAd).HasColumnType("varchar(50)");
                entity.Property(e => e.Aciklama).HasColumnType("varchar(150)");
            });

            builder.Entity<Alarmlar>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.AlarmAd).HasColumnType("varchar(100)");
                entity.Property(e => e.AlarmMetni).HasColumnType("varchar(300)");
                entity.Property(e => e.Aciklama).HasColumnType("varchar(150)");
                entity.Property(e => e.SesliMesajMetni).HasColumnType("varchar(450)");
                entity.Property(e => e.Tip).HasColumnType("varchar(10)");
                entity.Property(e => e.AlarmEslemeSozcugu).HasColumnType("varchar(100)");
            });

            builder.Entity<AlarmLog>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.KayitZamani).HasColumnType("datetime");
                entity.Property(e => e.Mesaj).HasColumnType("varchar(max)");
                entity.Property(e => e.PasifZamani).HasColumnType("datetime");
                entity.Property(e => e.GuncellemeZamani).HasColumnType("datetime");
            });

            builder.Entity<Bolum>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasColumnType("varchar(50)");
                entity.HasOne(p => p.Tesis)
                .WithMany(b => b.Bolumler)
                .HasForeignKey(p => p.TesisId);
            });

            builder.Entity<Form>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.Name).HasColumnType("varchar(50)");
                entity.Property(e => e.Link).HasColumnType("varchar(50)");
                entity.Property(e => e.Aciklama).HasColumnType("varchar(250)");
                entity.Property(e => e.ModulId).IsRequired();
            });

            builder.Entity<FormLokasyonEsleme>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.LokasyonId).IsRequired();
                entity.Property(e => e.FormId).IsRequired();
                entity.HasOne(p => p.Form)
                .WithMany(b => b.FormLokasyonEslemes)
                .HasForeignKey(p => p.FormId); // Form tablosundaki Id ile FormId sütunu arasında dış anahtar ilişkisi kur
                //entity.HasOne(p => p.Lokasyon)
                //.WithMany(b => b.FormLokasyonEslemes)
                //.HasForeignKey(p => p.LokasyonId); // Lokasyon tablosundaki Id ile LokasyonId sütunu arasında dış anahtar ilişkisi kur
            });

            builder.Entity<GenelParametreler>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.Name).HasColumnType("varchar(50)");
                entity.Property(e => e.Deger).HasColumnType("varchar(10)");
                entity.Property(e => e.Aciklama).HasColumnType("varchar(500)");
            });

            builder.Entity<GonderilenSms>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.KayitZamani).HasColumnType("datetime");
                entity.Property(e => e.CepTelefonu).HasColumnType("varchar(20)");
                entity.Property(e => e.Mesaj).HasColumnType("varchar(5000)");
                entity.Property(e => e.DonusMesaj).HasColumnType("varchar(5000)");
                entity.Property(e => e.NetIp).HasColumnType("varchar(30)");
                entity.Property(e => e.UserId).HasColumnType("nvarchar(450)");
                entity.Property(e => e.SonDenemeZamani).HasColumnType("datetime");
            });

            builder.Entity<Lokasyon>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasColumnType("varchar(50)");
                entity.Property(e => e.LokasyonNo).HasColumnType("varchar(50)");
                entity.Property(e => e.SirketId).IsRequired();
                entity.Property(e => e.Adres).HasColumnType("varchar(250)");

                entity.HasOne(p => p.Sirket) // Lokasyon sınıfındaki Sirket özelliğiyle ilişkilendir
                .WithMany(b => b.Lokasyons) // Sirket sınıfındaki Lokasyons koleksiyonu ile ilişkilendir
                .HasForeignKey(p => p.SirketId); // Lokasyon tablosundaki SirketId sütunu ile dış anahtar ilişkisi kur
            });

            builder.Entity<MailGonderimTakip>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.KayitZamani).HasColumnType("datetime");
                entity.Property(e => e.MailBasligi).HasColumnType("varchar(150)");
                entity.Property(e => e.MailIcerik).HasColumnType("varchar(MAX)");
                entity.Property(e => e.EkBilgileriJson).HasColumnType("varchar(MAX)");
                entity.Property(e => e.EkYolu).HasColumnType("varchar(350)");
                entity.Property(e => e.HataMesaji).HasColumnType("varchar(500)");
                entity.Property(e => e.GonderilecekTo).HasColumnType("varchar(MAX)");
                entity.Property(e => e.GonderilecekCc).HasColumnType("varchar(MAX)");
                entity.Property(e => e.SonDenemeZamani).HasColumnType("datetime");
                entity.Property(e => e.KalanDenemeSayisi).HasColumnType("smallint");
                entity.Property(e => e.UserId).HasColumnType("nvarchar(450)");
                entity.Property(e => e.KumesAlarmLogIds).HasColumnType("varchar(MAX)");
                entity.Property(e => e.KonveyorAlarmLogIds).HasColumnType("varchar(MAX)");
                entity.Property(e => e.SuHatAlarmLogIds).HasColumnType("varchar(MAX)");
            });

            builder.Entity<MesajGonderimTakip>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.KayitZamani).HasColumnType("datetime");
                entity.Property(e => e.SmsIcerik).HasColumnType("varchar(MAX)");
                entity.Property(e => e.HataMesaji).HasColumnType("varchar(MAX)");
                entity.Property(e => e.TelefonNo).HasColumnType("varchar(50)");
                entity.Property(e => e.SonDenemeZamani).HasColumnType("datetime");
                entity.Property(e => e.KalanDenemeSayisi).HasColumnType("smallint");
                entity.Property(e => e.UserId).HasColumnType("nvarchar(450)");
                entity.Property(e => e.KumesAlarmLogIds).HasColumnType("varchar(MAX)");
                entity.Property(e => e.KonveyorAlarmLogIds).HasColumnType("varchar(MAX)");
                entity.Property(e => e.SuHatAlarmLogIds).HasColumnType("varchar(MAX)");
                entity.Property(e => e.SesDosyaYolu).HasColumnType("varchar(500)");
                entity.Property(e => e.CallId).HasColumnType("varchar(500)");
                entity.Property(e => e.DurumKodu).HasColumnType("varchar(500)");
                entity.Property(e => e.DurumAciklama).HasColumnType("varchar(500)");
                entity.Property(e => e.BaslamaZamani).HasColumnType("datetime");
                entity.Property(e => e.BitisZamani).HasColumnType("datetime");
            });

            builder.Entity<MesajGonderimTakipAlarmLog>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.AlarmKaynakTuru).HasColumnType("varchar(30)");
                entity.Property(x => x.TakipTipi).HasColumnType("varchar(20)");
                entity.HasIndex(x => new { x.MesajGonderimTakipId, x.AlarmLogId, x.AlarmKaynakTuru, x.TakipTipi }).IsUnique(); // aynı alarm aynı takipte 2 kere yazılmasın
                entity.HasIndex(x => new { x.AlarmKaynakTuru, x.AlarmLogId });
            });

            builder.Entity<Modul>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.Name).HasColumnType("varchar(50)");
                entity.Property(e => e.Link).HasColumnType("varchar(50)");
                entity.Property(e => e.YetkiTablosu).HasColumnType("varchar(50)");
            });
           
            builder.Entity<ServisKontrol>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.ServisName).HasColumnType("varchar(150)");
                entity.Property(e => e.Mesaj).HasColumnType("varchar(500)");
                entity.Property(e => e.SonAktifZaman).HasColumnType("datetime");
                entity.Property(e => e.SonBasariliZaman).HasColumnType("datetime");
                entity.Property(e => e.ZamanAsimiDakika).HasColumnType("decimal(8,2)");
            });

            builder.Entity<Sirket>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.SirketNo).HasColumnType("varchar(50)");
                entity.Property(e => e.Name).HasColumnType("varchar(50)");
            });

            builder.Entity<SonGelistirmeler>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.VersiyonNo).HasColumnType("varchar(10)");
                entity.Property(e => e.Aciklama).HasColumnType("varchar(100)");
                entity.Property(e => e.Tarih).HasColumnType("datetime");
            });

            builder.Entity<Tesis>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.Name).HasColumnType("varchar(50)");
            });

            builder.Entity<UserFormYetki>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.UserId).HasColumnType("nvarchar(450)");
                entity.Property(e => e.SilinmeZamani).HasColumnType("datetime");
            });

            builder.Entity<UserEnerjiIzlemeYetki>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.UserId).HasColumnType("nvarchar(450)");
                entity.Property(e => e.SilinmeZamani).HasColumnType("datetime");
            });

            builder.Entity<UserMakineYetki>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.UserId).HasColumnType("nvarchar(450)");
                entity.Property(e => e.SilinmeZamani).HasColumnType("datetime");
            });

            builder.Entity<UserKumesYetki>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.UserId).HasColumnType("nvarchar(450)");
                entity.Property(e => e.SilinmeZamani).HasColumnType("datetime");
            });

            builder.Entity<UserLinkYetki>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.UserId).HasColumnType("nvarchar(450)");
                entity.Property(e => e.Link).HasColumnType("varchar(450)");
                entity.Property(e => e.SilinmeZamani).HasColumnType("datetime");
            });

            builder.Entity<UserSuKaynakYetki>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.UserId).HasColumnType("nvarchar(450)");
                entity.Property(e => e.SilinmeZamani).HasColumnType("datetime");
            });

            builder.Entity<YanginAlgilamaLogs>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.AlarmTip).HasColumnType("varchar(30)");
                entity.Property(e => e.KameraNo).HasColumnType("varchar(30)");
                //entity.Property(e => e.AnlikFotografDosyaYolu).HasColumnType("varchar(MAX)");
                //entity.Property(e => e.AnlikFotograf).HasColumnType("varbinary(MAX)");
                entity.Property(x => x.AnlikFotografDosyaYolu).HasColumnType("varchar(500)");
                entity.Property(x => x.AnlikFotografDosyaAdi).HasColumnType("varchar(255)");
                entity.Property(x => x.AnlikFotografContentType).HasColumnType("varchar(100)");
                entity.Property(e => e.KayitZamani).HasColumnType("datetime");
                entity.Property(e => e.NodeRedKayitZamani).HasColumnType("datetime");
                entity.HasOne(x => x.AlarmLog).WithMany(x => x.YanginAlgilamaLoglari).HasForeignKey(x => x.AlarmLogId).OnDelete(DeleteBehavior.Restrict);
                /* Mail için AlarmLogId üzerinden en son fotoğraf aranacağı için sorguyu hızlandırır. */
                entity.HasIndex(x => new { x.AlarmLogId, x.KayitZamani });
            });

            builder.Entity<vwAlarmKullaniciEsleme>().HasNoKey().ToView("vwAlarmKullaniciEsleme");
            builder.Entity<vwAlarmKullaniciEslemeForNotification>().HasNoKey().ToView("vwAlarmKullaniciEslemeForNotification");
            builder.Entity<vwAlarmlar>().HasNoKey().ToView("vwAlarmlar");
            builder.Entity<vwForm>().HasNoKey().ToView("vwForm");
            builder.Entity<vwUserFormYetki>().HasNoKey().ToView("vwUserFormYetki");

            builder.Entity<vwFormLokasyonEsleme>().HasNoKey().ToView("vwFormLokasyonEsleme");
            builder.Entity<vwUserEnerjiIzlemeYetki>().HasNoKey().ToView("vwUserEnerjiIzlemeYetki");
            builder.Entity<vwUserMakineYetki>().HasNoKey().ToView("vwUserMakineYetki");
            builder.Entity<vwUserKumesYetki>().HasNoKey().ToView("vwUserKumesYetki");
            builder.Entity<UserYetki>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.UserId).HasColumnType("nvarchar(450)");
                entity.Property(e => e.Link).HasColumnType("varchar(450)");
                entity.Property(e => e.SilinmeZamani).HasColumnType("datetime");
            });
            builder.Entity<vwUserLinkYetki>
                (entity =>
                {
                    entity.HasNoKey();
                    entity.ToView("vwUserLinkYetki");
                    entity.Ignore(x => x.LinkPathText);
                });

            builder.Entity<UserYetki>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.UserId).HasColumnType("nvarchar(450)");
                entity.Property(e => e.Link).HasColumnType("varchar(450)");
                entity.Property(e => e.SilinmeZamani).HasColumnType("datetime");
            });

            builder.Entity<FormLinkEsleme>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.FormId).IsRequired();
                entity.Property(e => e.Link).HasColumnType("varchar(450)").IsRequired();
                entity.Property(e => e.Ad).HasColumnType("nvarchar(200)");
                entity.HasOne(p => p.Form)
                    .WithMany()
                    .HasForeignKey(p => p.FormId);
                entity.HasOne(p => p.Sirket)
                    .WithMany()
                    .HasForeignKey(p => p.SirketId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            builder.Entity<vwUserSuKaynakYetki>().HasNoKey().ToView("vwUserSuKaynakYetki");
            builder.Entity<vwSuHatCizimSirketForYetki>().HasNoKey().ToView("vwSuHatCizimSirketForYetki");

            builder.Entity<Tbl_Makine>(e =>
            {
                e.HasKey(x => x.Id);
                e.ToTable("Tbl_Makine", "dbo", tb => tb.ExcludeFromMigrations());
            });

            builder.Entity<Tbl_Nesne>(e =>
            {
                e.HasKey(x => x.Id);
                e.ToTable("Tbl_Nesne", "dbo", tb => tb.ExcludeFromMigrations());
            });

            base.OnModelCreating(builder);
        }
    }
}