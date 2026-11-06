using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SuHatScada.Domain.DbViewModels;
using SuHatScada.Domain.Models;

namespace SuHatScada.Infrastructure
{
    public class SuHatScadaDbContext : DbContext
    {
        private IConfiguration _configuration;
        public SuHatScadaDbContext(DbContextOptions<DbContext> options) : base(options)
        {
        }

        public SuHatScadaDbContext(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var conStr = _configuration.GetConnectionString("SqlServer");
            if (conStr != null)
            {
                optionsBuilder.UseSqlServer(conStr);
            }
            else
            {
                optionsBuilder.UseSqlServer(_configuration.GetConnectionString("SuHatScadaSqlServer"));
            }
        }
        public DbSet<AlarmLog> AlarmLog { get; set; }
        public DbSet<Cizim> Cizim { get; set; }
        public DbSet<CizimLokasyonEsleme> CizimLokasyonEsleme { get; set; }
        public DbSet<CizimSekilEsleme> CizimSekilEsleme { get; set; }
        public DbSet<Etiket> Etiket { get; set; }
        public DbSet<EtiketLog> EtiketLog { get; set; }
        public DbSet<EtiketLogYeni> EtiketLogYeni { get; set; }
        public DbSet<EtiketLog_Temp> EtiketLog_Temp { get; set; }
        public DbSet<EtiketLog_TempYeni> EtiketLog_TempYeni { get; set; }
        public DbSet<KullaniciSekilYetki> KullaniciSekilYetki { get; set; }
        public DbSet<Logs> Logs { get; set; }
        public DbSet<PlcAyarlar> PlcAyarlar { get; set; }
        public DbSet<Sayac> Sayac { get; set; }
        public DbSet<SayacGunSonu> SayacGunSonu { get; set; }
        public DbSet<SayacLog> SayacLog { get; set; }
        public DbSet<Sekil> Sekil { get; set; }
        public DbSet<SekilEtiketKural> SekilEtiketKural { get; set; }
        public DbSet<SuIzlemeNodeRed> SuIzlemeNodeRed { get; set; }
        public DbSet<SuKaynak> SuKaynak { get; set; }
        public DbSet<SuKaynakLog> SuKaynakLog { get; set; }
        public DbSet<SuKaynakLogYeni> SuKaynakLogYeni { get; set; }
        public DbSet<SuKaynakLog_Temp> SuKaynakLog_Temp { get; set; }
        public DbSet<SuKaynakLog_TempYeni> SuKaynakLog_TempYeni { get; set; }
        public DbSet<vwAlarmLog> vwAlarmLog { get; set; }
        public DbSet<vwCizimLokasyonEsleme> vwCizimLokasyonEsleme { get; set; }
        public DbSet<vwLokasyon> vwLokasyon { get; set; }
        public DbSet<vwSuKaynakLog_Saatlik> vwSuKaynakLog_Saatlik { get; set; }
        public DbSet<vwSuKaynakLog_Gunluk> vwSuKaynakLog_Gunluk { get; set; }
        public DbSet<vwSuKaynakLog_Haftalik> vwSuKaynakLog_Haftalik { get; set; }
        public DbSet<vwSuKaynakLog_Aylik> vwSuKaynakLog_Aylik { get; set; }
        public DbSet<vwSuKaynakLog_Yillik> vwSuKaynakLog_Yillik { get; set; }
        public DbSet<vwTankLog_Saatlik> vwTankLog_Saatlik { get; set; }
        //public DbSet<vwCalismaSure_Saatlik> vwCalismaSure_Saatlik { get; set; }
        //public DbSet<vwCalismaSure_Gunluk> vwCalismaSure_Gunluk { get; set; }
        //public DbSet<vwCalismaSure_Haftalik> vwCalismaSure_Haftalik { get; set; }
        //public DbSet<vwCalismaSure_Aylik> vwCalismaSure_Aylik { get; set; }
        //public DbSet<vwCalismaSure_Yillik> vwCalismaSure_Yillik { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<AlarmLog>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.KayitZamani).HasColumnType("datetime");
                entity.Property(e => e.Mesaj).HasColumnType("varchar(MAX)");
            });

            builder.Entity<Cizim>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.Name).HasMaxLength(50);
                entity.Property(e => e.CizimTipi).HasColumnType("varchar(50)");
                entity.Property(e => e.CizimUrl).HasColumnType("varchar(350)");
                entity.Property(e => e.HmiUrl).HasColumnType("varchar(350)");
            });

            builder.Entity<CizimLokasyonEsleme>(entity =>
            {
                entity.HasKey(x => x.Id);
            });

            builder.Entity<CizimSekilEsleme>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasOne(p => p.Cizim)
                .WithMany(b => b.CizimSekilEslemes)
                .HasForeignKey(p => p.CizimId); // Cizim tablosundaki Id ile CizimId sütunu arasında dış anahtar ilişkisi kur

                entity.HasOne(p => p.Sekil)
               .WithMany(b => b.CizimSekilEslemes)
               .HasForeignKey(p => p.SekilId); // Sekil tablosundaki Id ile SekilId sütunu arasında dış anahtar ilişkisi kur
            });

            builder.Entity<Etiket>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.Name).HasColumnType("varchar(50)");
            });

            builder.Entity<EtiketLog>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.KayitZamani).HasColumnType("datetime");
                entity.Property(e => e.Deger).HasColumnType("decimal(18,3)");
            });

            builder.Entity<EtiketLogYeni>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.KayitZamani).HasColumnType("datetime");
                entity.Property(e => e.Deger).HasColumnType("decimal(18,3)");
                //entity.HasIndex(e => new { e.EtiketId, e.KayitZamani, e.Id }).IsDescending(false, true, true);
            });

            builder.Entity<EtiketLog_Temp>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.KayitZamani).HasColumnType("datetime");
                entity.Property(e => e.Deger).HasColumnType("decimal(18,3)");
            });

            builder.Entity<EtiketLog_TempYeni>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.KayitZamani).HasColumnType("datetime");
                entity.Property(e => e.Deger).HasColumnType("decimal(18,3)");
            });

            builder.Entity<KullaniciSekilYetki>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.UserId).HasColumnType("nvarchar(450)");
                entity.Property(e => e.RadioButonModTuru).HasColumnType("varchar(20)");
                entity.Property(e => e.ModTuru).HasColumnType("varchar(20)");
            });

            builder.Entity<Logs>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.KayitZamani).HasColumnType("datetime");
                entity.Property(e => e.Mesaj).HasColumnType("varchar(MAX)");
            });

            builder.Entity<PlcAyarlar>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.Name).HasColumnType("varchar(50)");
                entity.Property(e => e.PlcIpAdres).HasColumnType("varchar(20)");
                entity.Property(e => e.PlcNewIpAdres).HasColumnType("varchar(20)");
                entity.Property(e => e.ServisApiUrl).HasColumnType("varchar(50)");
            });

            builder.Entity<Sayac>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.Name).HasColumnType("varchar(50)");
                entity.Property(e => e.NodeRedGunSonuSayacName).HasColumnType("varchar(50)");
                entity.HasOne(s => s.FromSuKaynakNavigation)
                .WithMany(s => s.FromSayaclar)
                .HasForeignKey(s => s.FromSuKaynak);

                entity.HasOne(s => s.ToSuKaynakNavigation)
                .WithMany(s => s.ToSayaclar)
                .HasForeignKey(s => s.ToSuKaynak);
            });

            builder.Entity<SayacLog>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.KayitZamani).HasColumnType("datetime");
                entity.Property(e => e.Debi).HasColumnType("decimal(18,3)");
                entity.Property(e => e.Hiz).HasColumnType("decimal(18,3)");
                entity.Property(e => e.TotalDebi).HasColumnType("decimal(18,3)");
            });

            builder.Entity<SayacGunSonu>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.SayacAdi).HasColumnType("varchar(50)");
                entity.Property(e => e.KayitZamani).HasColumnType("datetime");
                entity.Property(e => e.SayacDegeri).HasColumnType("decimal(18,3)");
            });

            builder.Entity<Sekil>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.Name).HasColumnType("varchar(50)");
                entity.Property(e => e.Name).HasMaxLength(50);
                entity.Property(e => e.SekilJson).HasColumnType("varchar(MAX)");
            });

            builder.Entity<SekilEtiketKural>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.Name).HasColumnType("varchar(50)");
                entity.Property(e => e.Name).HasMaxLength(50);
                entity.Property(e => e.EtiketKuralJson).HasColumnType("varchar(MAX)");
                entity.Property(e => e.EtiketJson).HasColumnType("varchar(MAX)");
                entity.Property(e => e.MinDeger).HasColumnType("decimal(10,3)");
                entity.Property(e => e.MaxDeger).HasColumnType("decimal(10,3)");
                entity.Property(e => e.ModTuru).HasColumnType("varchar(20)");
                entity.HasOne(p => p.Sekil)
                .WithMany(b => b.EtiketKurals)
                .HasForeignKey(p => p.SekilId); // Sekil tablosundaki Id ile SekilId sütunu arasında dış anahtar ilişkisi kur
                entity.HasOne(p => p.Etiket)
               .WithMany(b => b.EtiketKurals)
               .HasForeignKey(p => p.EtiketId); // Etiket tablosundaki Id ile EtiketId sütunu arasında dış anahtar ilişkisi kur
            });

            builder.Entity<SuIzlemeNodeRed>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.KayitZamaniNodeRed).HasColumnType("datetime");
                entity.Property(e => e.KayitZamani).HasColumnType("datetime");
                entity.Property(e => e.Veriler).HasColumnType("varchar(MAX)");
            });

            builder.Entity<SuKaynak>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.Name).HasColumnType("varchar(50)");
            });

            builder.Entity<SuKaynakLog>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.KayitZamani).HasColumnType("datetime");
                entity.Property(e => e.Basinc).HasColumnType("decimal(18,3)");
                entity.Property(e => e.SeviyeMetre).HasColumnType("decimal(18,3)");
                entity.Property(e => e.SeviyeYuzde).HasColumnType("decimal(18,3)");
                entity.Property(e => e.SuHacim).HasColumnType("decimal(18,3)");
                entity.Property(e => e.SuHacimYuzde).HasColumnType("decimal(18,3)");
                entity.Property(e => e.TankHacim).HasColumnType("decimal(18,3)");
                entity.Property(e => e.DebiSayac).HasColumnType("decimal(18,3)");
                entity.Property(e => e.Debi).HasColumnType("decimal(18,3)");
            });

            builder.Entity<SuKaynakLogYeni>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.KayitZamani).HasColumnType("datetime");
                entity.Property(e => e.Basinc).HasColumnType("decimal(18,3)");
                entity.Property(e => e.SeviyeMetre).HasColumnType("decimal(18,3)");
                entity.Property(e => e.SeviyeYuzde).HasColumnType("decimal(18,3)");
                entity.Property(e => e.SuHacim).HasColumnType("decimal(18,3)");
                entity.Property(e => e.SuHacimYuzde).HasColumnType("decimal(18,3)");
                entity.Property(e => e.TankHacim).HasColumnType("decimal(18,3)");
                entity.Property(e => e.DebiSayac).HasColumnType("decimal(18,3)");
                entity.Property(e => e.Debi).HasColumnType("decimal(18,3)");
            });

            builder.Entity<SuKaynakLog_Temp>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.KayitZamani).HasColumnType("datetime");
                entity.Property(e => e.Basinc).HasColumnType("decimal(18,3)");
                entity.Property(e => e.SeviyeMetre).HasColumnType("decimal(18,3)");
                entity.Property(e => e.SeviyeYuzde).HasColumnType("decimal(18,3)");
                entity.Property(e => e.SuHacim).HasColumnType("decimal(18,3)");
                entity.Property(e => e.SuHacimYuzde).HasColumnType("decimal(18,3)");
                entity.Property(e => e.TankHacim).HasColumnType("decimal(18,3)");
                entity.Property(e => e.DebiSayac).HasColumnType("decimal(18,3)");
                entity.Property(e => e.Debi).HasColumnType("decimal(18,3)");
            });

            builder.Entity<SuKaynakLog_TempYeni>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.KayitZamani).HasColumnType("datetime");
                entity.Property(e => e.Basinc).HasColumnType("decimal(18,3)");
                entity.Property(e => e.SeviyeMetre).HasColumnType("decimal(18,3)");
                entity.Property(e => e.SeviyeYuzde).HasColumnType("decimal(18,3)");
                entity.Property(e => e.SuHacim).HasColumnType("decimal(18,3)");
                entity.Property(e => e.SuHacimYuzde).HasColumnType("decimal(18,3)");
                entity.Property(e => e.TankHacim).HasColumnType("decimal(18,3)");
                entity.Property(e => e.DebiSayac).HasColumnType("decimal(18,3)");
                entity.Property(e => e.Debi).HasColumnType("decimal(18,3)");
            });

            builder.Entity<vwAlarmLog>().HasNoKey().ToView("vwAlarmLog");
            builder.Entity<vwCizimLokasyonEsleme>().HasNoKey().ToView("vwCizimLokasyonEsleme");
            builder.Entity<vwLokasyon>().HasNoKey().ToView("vwLokasyon");

            builder.Entity<vwSuKaynakLog_Saatlik>(b =>
            {
                b.HasNoKey();
                b.ToView("vwSuKaynakLog_Saatlik");
                b.Property(x => x.Tarih).HasColumnType("date");
                b.Property(x => x.IlkZaman).HasColumnType("datetime");
                b.Property(x => x.SonZaman).HasColumnType("datetime");
                b.Property(x => x.IlkDeger).HasColumnType("decimal(18,3)");
                b.Property(x => x.SonDeger).HasColumnType("decimal(18,3)");
                b.Property(x => x.DebiFark).HasColumnType("decimal(19,3)");
            });
            builder.Entity<vwSuKaynakLog_Gunluk>(b =>
            {
                b.HasNoKey();
                b.ToView("vwSuKaynakLog_Gunluk");
                b.Property(x => x.Tarih).HasColumnType("date");
                b.Property(x => x.IlkZaman).HasColumnType("datetime");
                b.Property(x => x.SonZaman).HasColumnType("datetime");
                b.Property(x => x.IlkDeger).HasColumnType("decimal(18,3)");
                b.Property(x => x.SonDeger).HasColumnType("decimal(18,3)");
                b.Property(x => x.DebiFark).HasColumnType("decimal(19,3)");
            });
            builder.Entity<vwSuKaynakLog_Haftalik>(b =>
            {
                b.HasNoKey();
                b.ToView("vwSuKaynakLog_Haftalik");
                b.Property(x => x.IlkZaman).HasColumnType("datetime");
                b.Property(x => x.SonZaman).HasColumnType("datetime");
                b.Property(x => x.IlkDeger).HasColumnType("decimal(18,3)");
                b.Property(x => x.SonDeger).HasColumnType("decimal(18,3)");
                b.Property(x => x.DebiFark).HasColumnType("decimal(19,3)");
            });
            builder.Entity<vwSuKaynakLog_Aylik>(b =>
            {
                b.HasNoKey();
                b.ToView("vwSuKaynakLog_Aylik");
                b.Property(x => x.IlkZaman).HasColumnType("datetime");
                b.Property(x => x.SonZaman).HasColumnType("datetime");
                b.Property(x => x.IlkDeger).HasColumnType("decimal(18,3)");
                b.Property(x => x.SonDeger).HasColumnType("decimal(18,3)");
                b.Property(x => x.DebiFark).HasColumnType("decimal(19,3)");
            });
            builder.Entity<vwSuKaynakLog_Yillik>(b =>
            {
                b.HasNoKey();
                b.ToView("vwSuKaynakLog_Yillik");
                b.Property(x => x.IlkZaman).HasColumnType("datetime");
                b.Property(x => x.SonZaman).HasColumnType("datetime");
                b.Property(x => x.IlkDeger).HasColumnType("decimal(18,3)");
                b.Property(x => x.SonDeger).HasColumnType("decimal(18,3)");
                b.Property(x => x.DebiFark).HasColumnType("decimal(19,3)");
            });

            builder.Entity<vwTankLog_Saatlik>(b =>
            {
                b.HasNoKey();
                b.ToView("vwTankLog_Saatlik");

                b.Property(x => x.Tarih).HasColumnType("date");
                b.Property(x => x.IlkZaman).HasColumnType("datetime");
                b.Property(x => x.SonZaman).HasColumnType("datetime");
                b.Property(x => x.MinHacim).HasColumnType("decimal(18,3)");
                b.Property(x => x.OrtalamaHacim).HasColumnType("decimal(38,6)");
                b.Property(x => x.MaxHacim).HasColumnType("decimal(18,3)");
            });

            //builder.Entity<vwCalismaSure_Saatlik>(b =>
            //{
            //    b.HasNoKey();
            //    b.ToView("vwCalismaSure_Saatlik");
            //    b.Property(x => x.Periyot).HasColumnType("datetime");
            //    b.Property(x => x.IlkZaman).HasColumnType("datetime");
            //    b.Property(x => x.SonZaman).HasColumnType("datetime");
            //    b.Property(x => x.ToplamSaat).HasColumnType("numeric(17,6)");
            //});
            //builder.Entity<vwCalismaSure_Gunluk>(b =>
            //{
            //    b.HasNoKey();
            //    b.ToView("vwCalismaSure_Gunluk");
            //    b.Property(x => x.Periyot).HasColumnType("date");
            //    b.Property(x => x.IlkZaman).HasColumnType("datetime");
            //    b.Property(x => x.SonZaman).HasColumnType("datetime");
            //    b.Property(x => x.ToplamSaat).HasColumnType("numeric(17,6)");
            //});
            //builder.Entity<vwCalismaSure_Haftalik>(b =>
            //{
            //    b.HasNoKey();
            //    b.ToView("vwCalismaSure_Haftalik");
            //    b.Property(x => x.Periyot).HasColumnType("date");
            //    b.Property(x => x.IlkZaman).HasColumnType("datetime");
            //    b.Property(x => x.SonZaman).HasColumnType("datetime");
            //    b.Property(x => x.ToplamSaat).HasColumnType("numeric(17,6)");
            //});
            //builder.Entity<vwCalismaSure_Aylik>(b =>
            //{
            //    b.HasNoKey();
            //    b.ToView("vwCalismaSure_Aylik");
            //    b.Property(x => x.Periyot).HasColumnType("datetime");
            //    b.Property(x => x.ToplamSaat).HasColumnType("numeric(17,6)");
            //});
            //builder.Entity<vwCalismaSure_Yillik>(b =>
            //{
            //    b.HasNoKey();
            //    b.ToView("vwCalismaSure_Yillik");
            //    b.Property(x => x.Periyot).HasColumnType("datetime");
            //    b.Property(x => x.IlkZaman).HasColumnType("datetime");
            //    b.Property(x => x.SonZaman).HasColumnType("datetime");
            //    b.Property(x => x.ToplamSaat).HasColumnType("numeric(17,6)");
            //});

            base.OnModelCreating(builder);
        }
    }
}
