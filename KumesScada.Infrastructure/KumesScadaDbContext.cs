using KumesScada.Domain.DbViewModels;
using KumesScada.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SharedApp.Domain.DbViewModels;
using SharpCompress.Common;
using System.Reflection.Emit;

namespace KumesScada.Infrastructure
{
    public class KumesScadaDbContext : DbContext
    {
        private IConfiguration _configuration;

        public KumesScadaDbContext(DbContextOptions<DbContext> options)
      : base(options)
        {
        }

        public KumesScadaDbContext(DbContextOptions<KumesScadaDbContext> options)
      : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                var conStr = _configuration.GetConnectionString("KumesScadaSqlServer");
                optionsBuilder.UseSqlServer(conStr);
            }
            //var conStr = _configuration.GetConnectionString("SqlServer");
            //if (conStr != null)
            //{
            //    optionsBuilder.UseSqlServer(conStr);
            //}
            //else
            //{
            //    optionsBuilder.UseSqlServer(_configuration.GetConnectionString("KumesScadaSqlServer"));
            //}
        }
        public DbSet<AlarmLog> AlarmLog { get; set; }
        public DbSet<Cizim> Cizim { get; set; }
        public DbSet<CizimKumesEsleme> CizimKumesEsleme { get; set; }
        public DbSet<CizimLokasyonEsleme> CizimLokasyonEsleme { get; set; }
        public DbSet<CizimSekilEsleme> CizimSekilEsleme { get; set; }
        public DbSet<Donem> Donem { get; set; }
        public DbSet<Etiket> Etiket { get; set; }
        public DbSet<EtiketLog> EtiketLog { get; set; }
        public DbSet<EtiketLogYeni> EtiketLogYeni { get; set; }
        public DbSet<EtiketLog_Temp> EtiketLog_Temp { get; set; }
        public DbSet<EtiketLog_TempYeni> EtiketLog_TempYeni { get; set; }
        public DbSet<EtiketLogKonveyor> EtiketLogKonveyor { get; set; }
        public DbSet<HayvanCins> HayvanCins { get; set; }
        public DbSet<Konveyor> Konveyor { get; set; }
        public DbSet<KonveyorLog> KonveyorLog { get; set; }
        public DbSet<KonveyorRapor> KonveyorRapor { get; set; }
        public DbSet<Kumes> Kumes { get; set; }
        public DbSet<KumesEnerjiLog> KumesEnerjiLog { get; set; }
        public DbSet<KumesHaftalikCatiKontrol> KumesHaftalikCatiKontrol { get; set; }
        public DbSet<KumesHayvan> KumesHayvan { get; set; }
        public DbSet<KumesHayvanLog> KumesHayvanLog { get; set; }
        public DbSet<KumesLog> KumesLog { get; set; }
        public DbSet<KumesLogYeni> KumesLogYeni { get; set; }
        public DbSet<KumesLog_Temp> KumesLog_Temp { get; set; }
        public DbSet<KumesLog_TempYeni> KumesLog_TempYeni { get; set; }
        public DbSet<KumesIzlemeNodeRed> KumesIzlemeNodeRed { get; set; }
        public DbSet<KumesIzlemeNodeRed_mem> KumesIzlemeNodeRed_mem { get; set; }
        public DbSet<KumesRaporRehber> KumesRaporRehber { get; set; }
        public DbSet<KumesYemRasyonEsleme> KumesYemRasyonEsleme { get; set; }
        public DbSet<YemRasyon> YemRasyon { get; set; }
        public DbSet<YTKNodeRed> YTKNodeRed { get; set; }
        public DbSet<YTKNodeRed_mem> YTKNodeRed_mem { get; set; }
        public DbSet<Logs> Logs { get; set; }
        public DbSet<PlcBaglantiKesintiLog> PlcBaglantiKesintiLog { get; set; }
        public DbSet<Sekil> Sekil { get; set; }
        public DbSet<SekilEtiketKural> SekilEtiketKural { get; set; }
        public DbSet<vwLokasyon> vwLokasyon { get; set; }
        public DbSet<vwUserKumesYetkis> vwUserKumesYetkis { get; set; }
        public DbSet<vwPlcBaglantiKesintiLog> vwPlcBaglantiKesintiLog { get; set; }

        public DbSet<Kumesler> Kumesler { get; set; }
        public DbSet<TasnifMakinesi> TasnifMakineleri { get; set; }
        public DbSet<Olum> Olumler { get; set; }
        public DbSet<Yem> Yemler { get; set; }
        public DbSet<Su> Sular { get; set; }
        public DbSet<KaliteGizliCatlak> KaliteGizliCatlaklar { get; set; }
        public DbSet<KaliteMukavemet> KaliteMukavemetler { get; set; }
        public DbSet<CanliAgirlik> CanliAgirliklar { get; set; }
        public DbSet<YumurtaAgirlik> YumurtaAgirliklar { get; set; }
        public DbSet<Randiman> Randimanlar { get; set; }
        public DbSet<VbapRandiman> VbapRandimanlar { get; set; }
        public DbSet<Uygulama> Uygulamalar { get; set; }
        public DbSet<KumesVeri> KumesVerileri { get; set; }
        public DbSet<KumesHayvanDetay> KumesHayvanDetaylari { get; set; }
        public DbSet<KumesYumurtaDurum> KumesYumurtaDurumlari { get; set; }
        public DbSet<KumesYumurtaKaliteDurum> KumesYumurtaKaliteDurumlari { get; set; }
        public DbSet<RaporKategori> RaporKategorileri { get; set; }
        public DbSet<RaporVeri> RaporVerileri { get; set; }
        public DbSet<YumurtlamaDonem> YumurtlamaDonemleri { get; set; }

        public DbSet<NekropsiRaporu> NekropsiRaporlari { get; set; }
        public DbSet<NekropsiBulgu> NekropsiBulgulari { get; set; }
        public DbSet<NekropsiFotograf> NekropsiFotograflari { get; set; }

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
            });

            builder.Entity<CizimKumesEsleme>(entity =>
            {
                entity.HasKey(x => x.Id);
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

            builder.Entity<Donem>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.Name).HasColumnType("varchar(100)");
                entity.Property(e => e.GirisTarihi).HasColumnType("datetime");
                entity.Property(e => e.CikisTarihi).HasColumnType("datetime");
                entity.Property(e => e.GuncellemeTarihi).HasColumnType("datetime");
                entity.Property(e => e.GuncelleyenUserId).HasColumnType("nvarchar(450)");
                entity.Property(e => e.GuncellemeIslemi).HasColumnType("varchar(100)");
                entity.Property(e => e.Aciklama).HasColumnType("varchar(250)");
                entity.Property(e => e.Cins).HasColumnType("varchar(20)");
            });

            builder.Entity<Etiket>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.Name).HasColumnType("varchar(50)");
                entity.Property(e => e.EslemeSozcugu).HasColumnType("varchar(100)");
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

            builder.Entity<EtiketLogKonveyor>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.KayitZamani).HasColumnType("datetime");
                entity.Property(e => e.Deger).HasColumnType("decimal(18,3)");
            });

            builder.Entity<HayvanCins>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.Cins).HasColumnType("varchar(40)");
            });

            builder.Entity<Konveyor>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.Name).HasColumnType("varchar(50)");
            });

            builder.Entity<KonveyorLog>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.KayitZamani).HasColumnType("datetime");
                entity.Property(e => e.Aciklama).HasColumnType("varchar(500)");
            });

            builder.Entity<KonveyorRapor>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.KayitZamani).HasColumnType("datetime");
                entity.Property(e => e.Aciklama).HasColumnType("varchar(100)");
            });

            builder.Entity<Kumes>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.Name).HasColumnType("varchar(50)");
                entity.Property(e => e.KumesNo).HasColumnType("varchar(50)");
                entity.Property(e => e.KumesTcId).HasColumnType("varchar(50)");
                entity.Property(e => e.HmiIpAdres).HasColumnType("varchar(20)");
                entity.Property(e => e.PlcIpAdres).HasColumnType("varchar(20)");
                entity.Property(e => e.PlcNewIpAdres).HasColumnType("varchar(20)");
                entity.Property(e => e.ServisApiUrl).HasColumnType("varchar(50)");
                entity.Property(e => e.CizimUrl).HasColumnType("varchar(350)");
            });

            builder.Entity<KumesEnerjiLog>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.KayitZamani).HasColumnType("datetime");
                entity.Property(e => e.ImportAktifEnerji).HasColumnType("decimal(18,3)");
                entity.Property(e => e.TuketimBedeli).HasColumnType("decimal(18,3)");
            });

            builder.Entity<KumesHaftalikCatiKontrol>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.Deger).HasColumnType("decimal(10,3)");
                entity.Property(e => e.KayitZamani).HasColumnType("datetime");
                entity.Property(e => e.KayitYapanUserId).HasColumnType("nvarchar(450)");
                entity.Property(e => e.GuncellemeZamani).HasColumnType("datetime");
                entity.Property(e => e.GuncelleyenUserId).HasColumnType("nvarchar(450)");
            });
            
            builder.Entity<KumesHayvan>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.Cins).HasColumnType("varchar(20)");
            });

            builder.Entity<KumesHayvanLog>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.KayitZamani).HasColumnType("datetime");
                entity.Property(e => e.GirisZamani).HasColumnType("datetime");
                entity.Property(e => e.KayitYapanUserId).HasColumnType("nvarchar(450)");
                entity.Property(e => e.Aciklama).HasColumnType("varchar(150)");
            });

            builder.Entity<KumesLog>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.KayitZamani).HasColumnType("datetime");
                entity.Property(e => e.SicaklikKumes).HasColumnType("decimal(8,3)");
                entity.Property(e => e.SicaklikDis).HasColumnType("decimal(8,3)");
                entity.Property(e => e.Sicaklik1).HasColumnType("decimal(8,3)");
                entity.Property(e => e.Sicaklik2).HasColumnType("decimal(8,3)");
                entity.Property(e => e.Sicaklik3).HasColumnType("decimal(8,3)");
                entity.Property(e => e.Sicaklik4).HasColumnType("decimal(8,3)");
                entity.Property(e => e.Sicaklik5).HasColumnType("decimal(8,3)");
                entity.Property(e => e.Sicaklik6).HasColumnType("decimal(8,3)");
                entity.Property(e => e.Sicaklik7).HasColumnType("decimal(8,3)");
                entity.Property(e => e.Sicaklik8).HasColumnType("decimal(8,3)");
                entity.Property(e => e.NemKumes).HasColumnType("decimal(8,3)");
                entity.Property(e => e.NemDis).HasColumnType("decimal(8,3)");
                entity.Property(e => e.Nem1).HasColumnType("decimal(8,3)");
                entity.Property(e => e.Nem2).HasColumnType("decimal(8,3)");
                entity.Property(e => e.Nem3).HasColumnType("decimal(8,3)");
                entity.Property(e => e.Nem4).HasColumnType("decimal(8,3)");
                entity.Property(e => e.Nem5).HasColumnType("decimal(8,3)");
                entity.Property(e => e.Nem6).HasColumnType("decimal(8,3)");
                entity.Property(e => e.Nem7).HasColumnType("decimal(8,3)");
                entity.Property(e => e.Nem8).HasColumnType("decimal(8,3)");
                entity.Property(e => e.Silo1).HasColumnType("decimal(8,3)");
                entity.Property(e => e.Silo2).HasColumnType("decimal(8,3)");
                entity.Property(e => e.Basinc1).HasColumnType("decimal(8,3)");
                entity.Property(e => e.Co2_1).HasColumnType("decimal(8,3)");
                entity.Property(e => e.SuTuketimBugun).HasColumnType("decimal(8,3)");
                entity.Property(e => e.YemTuketimBugun).HasColumnType("decimal(8,3)");
            });

            builder.Entity<KumesLogYeni>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.KayitZamani).HasColumnType("datetime");
                entity.Property(e => e.SicaklikKumes).HasColumnType("decimal(8,3)");
                entity.Property(e => e.SicaklikDis).HasColumnType("decimal(8,3)");
                entity.Property(e => e.Sicaklik1).HasColumnType("decimal(8,3)");
                entity.Property(e => e.Sicaklik2).HasColumnType("decimal(8,3)");
                entity.Property(e => e.Sicaklik3).HasColumnType("decimal(8,3)");
                entity.Property(e => e.Sicaklik4).HasColumnType("decimal(8,3)");
                entity.Property(e => e.Sicaklik5).HasColumnType("decimal(8,3)");
                entity.Property(e => e.Sicaklik6).HasColumnType("decimal(8,3)");
                entity.Property(e => e.Sicaklik7).HasColumnType("decimal(8,3)");
                entity.Property(e => e.Sicaklik8).HasColumnType("decimal(8,3)");
                entity.Property(e => e.NemKumes).HasColumnType("decimal(8,3)");
                entity.Property(e => e.NemDis).HasColumnType("decimal(8,3)");
                entity.Property(e => e.Nem1).HasColumnType("decimal(8,3)");
                entity.Property(e => e.Nem2).HasColumnType("decimal(8,3)");
                entity.Property(e => e.Nem3).HasColumnType("decimal(8,3)");
                entity.Property(e => e.Nem4).HasColumnType("decimal(8,3)");
                entity.Property(e => e.Nem5).HasColumnType("decimal(8,3)");
                entity.Property(e => e.Nem6).HasColumnType("decimal(8,3)");
                entity.Property(e => e.Nem7).HasColumnType("decimal(8,3)");
                entity.Property(e => e.Nem8).HasColumnType("decimal(8,3)");
                entity.Property(e => e.Silo1).HasColumnType("decimal(8,3)");
                entity.Property(e => e.Silo2).HasColumnType("decimal(8,3)");
                entity.Property(e => e.Basinc1).HasColumnType("decimal(8,3)");
                entity.Property(e => e.Co2_1).HasColumnType("decimal(8,3)");
                entity.Property(e => e.SuTuketimBugun).HasColumnType("decimal(8,3)");
                entity.Property(e => e.YemTuketimBugun).HasColumnType("decimal(8,3)");
                entity.Property(e => e.PedSuTuketimBugun).HasColumnType("decimal(8,3)");
                entity.Property(e => e.IcmeSuyuSayac).HasColumnType("decimal(18,3)");
                entity.Property(e => e.PedSuSayac).HasColumnType("decimal(18,3)");
                entity.Property(e => e.SapGunlukYemGirisi).HasColumnType("decimal(12,3)");
                entity.Property(e => e.HesaplananYemTuketimi).HasColumnType("decimal(12,3)");
            });

            builder.Entity<KumesLog_Temp>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.KayitZamani).HasColumnType("datetime");
                entity.Property(e => e.SicaklikKumes).HasColumnType("decimal(8,3)");
                entity.Property(e => e.SicaklikDis).HasColumnType("decimal(8,3)");
                entity.Property(e => e.Sicaklik1).HasColumnType("decimal(8,3)");
                entity.Property(e => e.Sicaklik2).HasColumnType("decimal(8,3)");
                entity.Property(e => e.Sicaklik3).HasColumnType("decimal(8,3)");
                entity.Property(e => e.Sicaklik4).HasColumnType("decimal(8,3)");
                entity.Property(e => e.Sicaklik5).HasColumnType("decimal(8,3)");
                entity.Property(e => e.Sicaklik6).HasColumnType("decimal(8,3)");
                entity.Property(e => e.Sicaklik7).HasColumnType("decimal(8,3)");
                entity.Property(e => e.Sicaklik8).HasColumnType("decimal(8,3)");
                entity.Property(e => e.NemKumes).HasColumnType("decimal(8,3)");
                entity.Property(e => e.NemDis).HasColumnType("decimal(8,3)");
                entity.Property(e => e.Nem1).HasColumnType("decimal(8,3)");
                entity.Property(e => e.Nem2).HasColumnType("decimal(8,3)");
                entity.Property(e => e.Nem3).HasColumnType("decimal(8,3)");
                entity.Property(e => e.Nem4).HasColumnType("decimal(8,3)");
                entity.Property(e => e.Nem5).HasColumnType("decimal(8,3)");
                entity.Property(e => e.Nem6).HasColumnType("decimal(8,3)");
                entity.Property(e => e.Nem7).HasColumnType("decimal(8,3)");
                entity.Property(e => e.Nem8).HasColumnType("decimal(8,3)");
                entity.Property(e => e.Silo1).HasColumnType("decimal(8,3)");
                entity.Property(e => e.Silo2).HasColumnType("decimal(8,3)");
                entity.Property(e => e.Basinc1).HasColumnType("decimal(8,3)");
                entity.Property(e => e.Co2_1).HasColumnType("decimal(8,3)");
                entity.Property(e => e.SuTuketimBugun).HasColumnType("decimal(8,3)");
                entity.Property(e => e.YemTuketimBugun).HasColumnType("decimal(8,3)");
            });

            builder.Entity<KumesLog_TempYeni>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.KayitZamani).HasColumnType("datetime");
                entity.Property(e => e.SicaklikKumes).HasColumnType("decimal(8,3)");
                entity.Property(e => e.SicaklikDis).HasColumnType("decimal(8,3)");
                entity.Property(e => e.Sicaklik1).HasColumnType("decimal(8,3)");
                entity.Property(e => e.Sicaklik2).HasColumnType("decimal(8,3)");
                entity.Property(e => e.Sicaklik3).HasColumnType("decimal(8,3)");
                entity.Property(e => e.Sicaklik4).HasColumnType("decimal(8,3)");
                entity.Property(e => e.Sicaklik5).HasColumnType("decimal(8,3)");
                entity.Property(e => e.Sicaklik6).HasColumnType("decimal(8,3)");
                entity.Property(e => e.Sicaklik7).HasColumnType("decimal(8,3)");
                entity.Property(e => e.Sicaklik8).HasColumnType("decimal(8,3)");
                entity.Property(e => e.NemKumes).HasColumnType("decimal(8,3)");
                entity.Property(e => e.NemDis).HasColumnType("decimal(8,3)");
                entity.Property(e => e.Nem1).HasColumnType("decimal(8,3)");
                entity.Property(e => e.Nem2).HasColumnType("decimal(8,3)");
                entity.Property(e => e.Nem3).HasColumnType("decimal(8,3)");
                entity.Property(e => e.Nem4).HasColumnType("decimal(8,3)");
                entity.Property(e => e.Nem5).HasColumnType("decimal(8,3)");
                entity.Property(e => e.Nem6).HasColumnType("decimal(8,3)");
                entity.Property(e => e.Nem7).HasColumnType("decimal(8,3)");
                entity.Property(e => e.Nem8).HasColumnType("decimal(8,3)");
                entity.Property(e => e.Silo1).HasColumnType("decimal(8,3)");
                entity.Property(e => e.Silo2).HasColumnType("decimal(8,3)");
                entity.Property(e => e.Basinc1).HasColumnType("decimal(8,3)");
                entity.Property(e => e.Co2_1).HasColumnType("decimal(8,3)");
                entity.Property(e => e.SuTuketimBugun).HasColumnType("decimal(8,3)");
                entity.Property(e => e.YemTuketimBugun).HasColumnType("decimal(8,3)");
                entity.Property(e => e.PedSuTuketimBugun).HasColumnType("decimal(8,3)");
            });

            builder.Entity<KumesIzlemeNodeRed>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.KayitZamaniNodeRed).HasColumnType("datetime");
                entity.Property(e => e.KayitZamani).HasColumnType("datetime");
                entity.Property(e => e.Veriler).HasColumnType("varchar(MAX)");
            });

            builder.Entity<KumesIzlemeNodeRed_mem>(entity =>
            {
                entity.ToTable("KumesIzlemeNodeRed_mem");
                entity.IsMemoryOptimized();
                entity.Metadata.SetIsTableExcludedFromMigrations(true);
                entity.HasKey(x => x.Id);
                entity.Property(e => e.KayitZamaniNodeRed).HasColumnType("datetime");
                entity.Property(e => e.KayitZamani).HasColumnType("datetime");
                //entity.Property(e => e.Veriler).HasColumnType("varchar(MAX)");
            });

            builder.Entity<KumesRaporRehber>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.KayitZamani).HasColumnType("datetime");
                entity.Property(e => e.GuncellemeZamani).HasColumnType("datetime");
                entity.Property(e => e.KaydedenUserId).HasColumnType("nvarchar(450)");
                entity.Property(e => e.KaydedenUserId).HasColumnType("nvarchar(450)");
                entity.Property(e => e.RehberAdi).HasColumnType("varchar(70)");
                entity.Property(e => e.Deger).HasColumnType("decimal(10,3)");
            });
            
            builder.Entity<KumesYemRasyonEsleme>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.KayitZamani).HasColumnType("datetime");
                entity.Property(e => e.GecerlilikBaslangicZamani).HasColumnType("datetime");
                entity.Property(e => e.GecerlilikBitisZamani).HasColumnType("datetime");
            });

            builder.Entity<YemRasyon>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.YemAdi).HasColumnType("varchar(70)");
                entity.Property(e => e.Rasyon).HasColumnType("varchar(70)");
                entity.Property(e => e.BirimFiyat).HasColumnType("decimal(15,3)");
                entity.Property(e => e.GecerlilikBaslangicZamani).HasColumnType("datetime");
                entity.Property(e => e.GecerlilikBitisZamani).HasColumnType("datetime");
            });

            builder.Entity<YTKNodeRed_mem>(entity =>
            {
                entity.ToTable("YTKNodeRed_mem");
                entity.IsMemoryOptimized();
                entity.Metadata.SetIsTableExcludedFromMigrations(true);
                entity.HasKey(x => x.Id);
                entity.Property(e => e.KayitZamaniNodeRed).HasColumnType("datetime");
                entity.Property(e => e.KayitZamani).HasColumnType("datetime");
                //entity.Property(e => e.Veriler).HasColumnType("varchar(MAX)");
                entity.Property(e => e.PlcAdi).HasColumnType("varchar(50)");
            });

            builder.Entity<YTKNodeRed>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.KayitZamaniNodeRed).HasColumnType("datetime");
                entity.Property(e => e.KayitZamani).HasColumnType("datetime");
                entity.Property(e => e.Veriler).HasColumnType("varchar(MAX)");
                entity.Property(e => e.PlcAdi).HasColumnType("varchar(50)");
            });

            builder.Entity<Logs>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.KayitZamani).HasColumnType("datetime");
                entity.Property(e => e.Mesaj).HasColumnType("varchar(MAX)");
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
                entity.Property(e => e.MinDeger).HasColumnType("decimal(10,3)");
                entity.Property(e => e.MaxDeger).HasColumnType("decimal(10,3)");
                entity.HasOne(p => p.Sekil)
                .WithMany(b => b.EtiketKurals)
                .HasForeignKey(p => p.SekilId); // Sekil tablosundaki Id ile SekilId sütunu arasında dış anahtar ilişkisi kur
                entity.HasOne(p => p.Etiket)
               .WithMany(b => b.EtiketKurals)
               .HasForeignKey(p => p.EtiketId); // Etiket tablosundaki Id ile EtiketId sütunu arasında dış anahtar ilişkisi kur
            });
            builder.Entity<PlcBaglantiKesintiLog>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.KopmaZamani).HasColumnType("datetime");
                entity.Property(e => e.BaglanmaZamani).HasColumnType("datetime");
                // Tablo migration ile değil, doğrudan DB üzerinde oluşturuldu.
                entity.Metadata.SetIsTableExcludedFromMigrations(true);
            });

            builder.Entity<vwLokasyon>().HasNoKey().ToView("vwLokasyon");
            builder.Entity<vwUserKumesYetkis>().HasNoKey().ToView("vwUserKumesYetkis");
            builder.Entity<vwPlcBaglantiKesintiLog>(entity =>
            {
                entity.HasNoKey().ToView("vwPlcBaglantiKesintiLog");
                // Hassasiyet belirtilmezse EF her model oluşturulduğunda
                // "No store type was specified for the decimal property" uyarısı
                // basıyor. Değerler migration snapshot'taki varsayılanla aynı
                // (decimal(18,2)) - şema değişmez.
                entity.Property(x => x.ToplamKesintiDakika).HasPrecision(18, 2);
                entity.Property(x => x.OrtalamaKesintiSaniye).HasPrecision(18, 2);
            });

            builder.Entity<YumurtlamaDonem>(entity =>
            {
                // Tablo migration 37'de decimal(18,2) olarak oluşturuldu -
                // aynı hassasiyet açıkça verildi, şema değişmez (sadece uyarıyı kaldırır).
                entity.Property(x => x.BaslangicHayvanSayisi).HasPrecision(18, 2);
            });


            builder.Entity<Kumesler>(entity =>
            {
                entity.ToTable("Kumesler");

                entity.HasKey(x => x.Id);

                entity.Property(x => x.Id)
                    .ValueGeneratedOnAdd();

                entity.Property(x => x.KumesNo)
                    .HasMaxLength(50);

                entity.Property(x => x.KumesName)
                    .HasMaxLength(250);

                entity.Property(x => x.Aktif)
                    .HasDefaultValue(true);

                entity.Property(x => x.Silindi)
                    .HasDefaultValue(false);

                // Dış sistem / mevcut sistemdeki KumesId ise index koyabiliriz.
                entity.HasIndex(x => x.KumesId);

                entity.HasIndex(x => x.LokasyonId);

                // Tasnif Makinesi
                entity.HasOne(x => x.TasnifMakinesi)
                    .WithMany(x => x.Kumesler)
                    .HasForeignKey(x => x.TasnifMakinesiId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<TasnifMakinesi>(entity =>
            {
                entity.ToTable("TasnifMakinesi");

                entity.HasKey(x => x.TasnifMakinesiId);

                entity.Property(x => x.TasnifMakinesiId)
                    .ValueGeneratedOnAdd();

                entity.Property(x => x.TasnifMakinesiAdi)
                    .HasMaxLength(250);
            });

            builder.Entity<Olum>(entity =>
            {
                entity.ToTable("Olum");

                entity.HasKey(x => x.OlumId);

                entity.Property(x => x.OlumId)
                    .ValueGeneratedOnAdd();

                entity.Property(x => x.OlumMiktar)
                    .HasPrecision(18, 2);

                entity.HasOne(x => x.Kumesler)
                    .WithMany(x => x.Olumler)
                    .HasForeignKey(x => x.KumeslerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<Yem>(entity =>
            {
                entity.ToTable("Yem");

                entity.HasKey(x => x.YemId);

                entity.Property(x => x.YemId)
                    .ValueGeneratedOnAdd();

                entity.Property(x => x.YemMiktar)
                    .HasPrecision(18, 3);

                entity.HasOne(x => x.Kumesler)
                    .WithMany(x => x.Yemler)
                    .HasForeignKey(x => x.KumeslerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<Su>(entity =>
            {
                entity.ToTable("Su");

                entity.HasKey(x => x.SuId);

                entity.Property(x => x.SuId)
                    .ValueGeneratedOnAdd();

                entity.Property(x => x.SuMiktar)
                    .HasPrecision(18, 3);

                entity.HasOne(x => x.Kumesler)
                    .WithMany(x => x.Sular)
                    .HasForeignKey(x => x.KumeslerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<KaliteGizliCatlak>(entity =>
            {
                entity.ToTable("KaliteGizliCatlak");

                entity.HasKey(x => x.KaliteGizliCatlakId);

                entity.Property(x => x.KaliteGizliCatlakId)
                    .ValueGeneratedOnAdd();

                entity.Property(x => x.KaliteGizliCatlakMiktar)
                    .HasPrecision(18, 3);

                entity.HasOne(x => x.Kumesler)
                    .WithMany(x => x.KaliteGizliCatlaklar)
                    .HasForeignKey(x => x.KumeslerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<KaliteMukavemet>(entity =>
            {
                entity.ToTable("KaliteMukavemet");

                entity.HasKey(x => x.KaliteMukavemetId);

                entity.Property(x => x.KaliteMukavemetId)
                    .ValueGeneratedOnAdd();

                entity.Property(x => x.KaliteMukavemetMiktar)
                    .HasPrecision(18, 3);

                entity.HasOne(x => x.Kumesler)
                    .WithMany(x => x.KaliteMukavemetler)
                    .HasForeignKey(x => x.KumeslerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<CanliAgirlik>(entity =>
            {
                entity.ToTable("CanliAgirlik");

                entity.HasKey(x => x.CanliAgirlikId);

                entity.Property(x => x.CanliAgirlikId)
                    .ValueGeneratedOnAdd();

                entity.Property(x => x.CanliAgirlikMiktar)
                    .HasPrecision(18, 3);

                entity.HasOne(x => x.Kumesler)
                    .WithMany(x => x.CanliAgirliklar)
                    .HasForeignKey(x => x.KumeslerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<YumurtaAgirlik>(entity =>
            {
                entity.ToTable("YumurtaAgirlik");

                entity.HasKey(x => x.YumurtaAgirlikId);

                entity.Property(x => x.YumurtaAgirlikId)
                    .ValueGeneratedOnAdd();

                entity.Property(x => x.YumurtaAgirlikMiktar)
                    .HasPrecision(18, 3);

                entity.HasOne(x => x.Kumesler)
                    .WithMany(x => x.YumurtaAgirliklar)
                    .HasForeignKey(x => x.KumeslerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<Randiman>(entity =>
            {
                entity.ToTable("Randiman");

                entity.HasKey(x => x.RandimanId);

                entity.Property(x => x.RandimanId)
                    .ValueGeneratedOnAdd();

                entity.Property(x => x.RandimanMiktar)
                    .HasPrecision(18, 4);

                entity.HasOne(x => x.Kumesler)
                    .WithMany(x => x.Randimanlar)
                    .HasForeignKey(x => x.KumeslerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<VbapRandiman>(entity =>
            {
                entity.ToTable("VbapRandiman");

                entity.HasKey(x => x.VbapRandimanId);

                entity.Property(x => x.VbapRandimanId)
                    .ValueGeneratedOnAdd();

                entity.Property(x => x.VbapRandimanMiktar)
                    .HasPrecision(18, 4);

                entity.HasOne(x => x.Kumesler)
                    .WithMany(x => x.VbapRandimanlar)
                    .HasForeignKey(x => x.KumeslerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<Uygulama>(entity =>
            {
                entity.ToTable("Uygulama");

                entity.HasKey(x => x.UygulamalarId);

                entity.Property(x => x.UygulamalarId)
                    .ValueGeneratedOnAdd();

                entity.Property(x => x.UygulamaAciklama)
                    .HasMaxLength(1000);

                entity.Property(x => x.Tip)
                    .HasMaxLength(50);

                entity.HasOne(x => x.Kumesler)
                    .WithMany(x => x.Uygulamalar)
                    .HasForeignKey(x => x.KumeslerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<KumesVeri>(entity =>
            {
                entity.ToTable("KumesVeri");

                entity.HasKey(x => x.KumesVeriId);

                entity.Property(x => x.KumesVeriId)
                    .ValueGeneratedOnAdd();

                entity.Property(x => x.SiparisNo)
                    .HasMaxLength(100);

                entity.Property(x => x.KumesHayvanDetayDeger)
                    .HasPrecision(18, 3);

                entity.Property(x => x.KumesYumurtaDurumDeger)
                    .HasPrecision(18, 3);

                entity.Property(x => x.KumesYumurtaKaliteDurumDeger)
                    .HasPrecision(18, 3);

                entity.HasOne(x => x.Kumesler)
                    .WithMany(x => x.KumesVerileri)
                    .HasForeignKey(x => x.KumeslerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.KumesHayvanDetay)
                    .WithMany(x => x.KumesVerileri)
                    .HasForeignKey(x => x.KumesHayvanDetayId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.KumesYumurtaDurum)
                    .WithMany(x => x.KumesVerileri)
                    .HasForeignKey(x => x.KumesYumurtaDurumId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.KumesYumurtaKaliteDurum)
                    .WithMany(x => x.KumesVerileri)
                    .HasForeignKey(x => x.KumesYumurtaKaliteDurumId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<KumesHayvanDetay>(entity =>
            {
                entity.ToTable("KumesHayvanDetay");

                entity.HasKey(x => x.KumesHayvanDetayId);

                entity.Property(x => x.KumesHayvanDetayAdi)
                    .HasMaxLength(250);
            });

            builder.Entity<KumesYumurtaDurum>(entity =>
            {
                entity.ToTable("KumesYumurtaDurum");

                entity.HasKey(x => x.KumesYumurtaDurumId);

                entity.Property(x => x.KumesYumurtaDurumAd)
                    .HasMaxLength(250);
            });

            builder.Entity<KumesYumurtaKaliteDurum>(entity =>
            {
                entity.ToTable("KumesYumurtaKaliteDurum");

                entity.HasKey(x => x.KumesYumurtaKaliteDurumId);

                entity.Property(x => x.KumesYumurtaKaliteDurumAd)
                    .HasMaxLength(250);
            });
 
            builder.Entity<RaporKategori>(entity =>
            {
                entity.ToTable("RaporKategori");

                entity.HasKey(x => x.RaporKategoriId);

                entity.Property(x => x.RaporKategoriAd)
                    .HasMaxLength(250);
            });


            builder.Entity<RaporVeri>(entity =>
            {
                entity.ToTable("RaporVeri");

                entity.HasKey(x => x.RaporVerilerId);

                entity.Property(x => x.RaporHedefDeger)
                    .HasPrecision(18, 4);

                entity.HasOne(x => x.RaporKategori)
                    .WithMany(x => x.RaporVerileri)
                    .HasForeignKey(x => x.RaporKategoriId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(x => new { x.RaporKategoriId, x.HaftaSayisi })
                    .IsUnique();
            });

            builder.Entity<NekropsiRaporu>(entity =>
            {
                entity.ToTable("NekropsiRaporu");

                entity.HasKey(x => x.Id);

                entity.Property(x => x.NekropsiSebebi)
                    .IsRequired()
                    .HasColumnType("varchar(500)");

                entity.Property(x => x.Pcr)
                    .IsRequired()
                    .HasColumnType("varchar(20)")
                    .HasDefaultValue("BELIRTILMEDI");

                entity.Property(x => x.Tarih).HasColumnType("datetime");
                entity.Property(x => x.KayitZamani).HasColumnType("datetime");
                entity.Property(x => x.KayitYapanUserId).HasColumnType("nvarchar(450)");

                entity.Property(x => x.Silindi).HasDefaultValue(false);

                entity.HasIndex(x => x.KumesId);
            });

            builder.Entity<NekropsiBulgu>(entity =>
            {
                entity.ToTable("NekropsiBulgu");

                entity.HasKey(x => x.Id);

                entity.Property(x => x.Key)
                    .IsRequired()
                    .HasColumnType("varchar(100)");

                entity.HasOne(x => x.NekropsiRaporu)
                    .WithMany(x => x.Bulgular)
                    .HasForeignKey(x => x.NekropsiRaporuId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<NekropsiFotograf>(entity =>
            {
                entity.ToTable("NekropsiFotograf");

                entity.HasKey(x => x.Id);

                entity.Property(x => x.DosyaAdi)
                    .IsRequired()
                    .HasColumnType("varchar(260)");

                entity.Property(x => x.GoreliDosyaYolu)
                    .IsRequired()
                    .HasColumnType("varchar(400)");

                entity.Property(x => x.ContentType)
                    .IsRequired()
                    .HasColumnType("varchar(100)");

                entity.Property(x => x.KayitZamani).HasColumnType("datetime");

                entity.HasOne(x => x.NekropsiRaporu)
                    .WithMany(x => x.Fotograflar)
                    .HasForeignKey(x => x.NekropsiRaporuId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            base.OnModelCreating(builder);
        }
    }
}