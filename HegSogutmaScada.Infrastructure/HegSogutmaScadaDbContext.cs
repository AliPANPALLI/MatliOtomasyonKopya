using HegSogutmaScada.Domain.DbViewModels;
using HegSogutmaScada.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace HegSogutmaScada.Infrastructure
{
    // Bu context başka bir ekibin yönettiği, uzak sunucudaki mevcut TimescaleDB
    // şemasını (heg_gida_sogutma) salt okunur olarak eşler. Bilerek migration
    // içermiyor - şema o taraftaki DDL/pg_cron job'larıyla yönetiliyor, buradan
    // asla migrate edilmemeli. Bağlantı Program.cs'de AddDbContext ile tek
    // seferde kuruluyor; ikinci bir (IConfiguration alan) constructor bilerek
    // yok - iki constructor DI'da "ambiguous" hatasına yol açıyordu.
    public class HegSogutmaScadaDbContext : DbContext
    {
        public HegSogutmaScadaDbContext(DbContextOptions<HegSogutmaScadaDbContext> options) : base(options)
        {
        }

        public DbSet<Kompresor> Kompresorler => Set<Kompresor>();
        public DbSet<KompresorAnlik> KompresorAnlik => Set<KompresorAnlik>();
        public DbSet<Oda> Odalar => Set<Oda>();
        public DbSet<OdaVanasi> OdaVanalari => Set<OdaVanasi>();
        public DbSet<VanaAnlik> VanaAnlik => Set<VanaAnlik>();

        public DbSet<KompresorOkumaSaatlik> KompresorOkumaSaatlik => Set<KompresorOkumaSaatlik>();
        public DbSet<KompresorOkumaGunluk> KompresorOkumaGunluk => Set<KompresorOkumaGunluk>();
        public DbSet<KompresorOkumaAylik> KompresorOkumaAylik => Set<KompresorOkumaAylik>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Kompresor>(entity =>
            {
                entity.ToTable("kompresorler");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).HasColumnName("id");
                entity.Property(x => x.TagNo).HasColumnName("tag_no");
                entity.Property(x => x.Ad).HasColumnName("ad");
                entity.Property(x => x.PlcKapasiteTag).HasColumnName("plc_kapasite_tag");
                entity.Property(x => x.PlcCalismaSaatiTag).HasColumnName("plc_calisma_saati_tag");
                entity.Property(x => x.PlcKapasiteAdres).HasColumnName("plc_kapasite_adres");
                entity.Property(x => x.PlcCalismaSaatiAdres).HasColumnName("plc_calisma_saati_adres");
            });

            builder.Entity<KompresorAnlik>(entity =>
            {
                entity.ToTable("kompresor_anlik");
                entity.HasKey(x => x.KompresorId);
                entity.Property(x => x.KompresorId).HasColumnName("kompresor_id");
                entity.Property(x => x.KapasiteYuzde).HasColumnName("kapasite_yuzde");
                entity.Property(x => x.CalismaSaatiHam).HasColumnName("calisma_saati_ham");
                entity.Property(x => x.CalismaSaatiNet).HasColumnName("calisma_saati_net");
                entity.Property(x => x.GuncellemeZamani).HasColumnName("guncelleme_zamani");
            });

            builder.Entity<Oda>(entity =>
            {
                entity.ToTable("odalar");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).HasColumnName("id");
                entity.Property(x => x.Kat).HasColumnName("kat");
                entity.Property(x => x.Ad).HasColumnName("ad");
            });

            builder.Entity<OdaVanasi>(entity =>
            {
                entity.ToTable("oda_vanalari");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).HasColumnName("id");
                entity.Property(x => x.OdaId).HasColumnName("oda_id");
                entity.Property(x => x.PlcTag).HasColumnName("plc_tag");
                entity.Property(x => x.PlcAdres).HasColumnName("plc_adres");
                entity.Property(x => x.Aciklama).HasColumnName("aciklama");
                entity.Property(x => x.EkipmanKodu).HasColumnName("ekipman_kodu");
            });

            builder.Entity<VanaAnlik>(entity =>
            {
                entity.ToTable("vana_anlik");
                entity.HasKey(x => x.VanaId);
                entity.Property(x => x.VanaId).HasColumnName("vana_id");
                entity.Property(x => x.Durum).HasColumnName("durum");
                entity.Property(x => x.DurumBaslangici).HasColumnName("durum_baslangici");
                entity.Property(x => x.GuncellemeZamani).HasColumnName("guncelleme_zamani");
            });

            ConfigureBucketView<KompresorOkumaSaatlik>(builder, "kompresor_okuma_saatlik");
            ConfigureBucketView<KompresorOkumaGunluk>(builder, "kompresor_okuma_gunluk");
            ConfigureBucketView<KompresorOkumaAylik>(builder, "kompresor_okuma_aylik");
        }

        private static void ConfigureBucketView<TEntity>(ModelBuilder builder, string viewName)
            where TEntity : KompresorOkumaBucketBase
        {
            builder.Entity<TEntity>(entity =>
            {
                entity.HasNoKey();
                entity.ToView(viewName);
                entity.Property(x => x.KompresorId).HasColumnName("kompresor_id");
                entity.Property(x => x.Bucket).HasColumnName("bucket");
                entity.Property(x => x.OrtKapasite).HasColumnName("ort_kapasite");
                entity.Property(x => x.MinKapasite).HasColumnName("min_kapasite");
                entity.Property(x => x.MaksKapasite).HasColumnName("maks_kapasite");
                entity.Property(x => x.SaatBaslangicHam).HasColumnName("saat_baslangic_ham");
                entity.Property(x => x.SaatBitisHam).HasColumnName("saat_bitis_ham");
                entity.Property(x => x.SaatBaslangicNet).HasColumnName("saat_baslangic_net");
                entity.Property(x => x.SaatBitisNet).HasColumnName("saat_bitis_net");
                entity.Property(x => x.SaatFark).HasColumnName("saat_fark");
                entity.Property(x => x.OrnekSayisi).HasColumnName("ornek_sayisi");
            });
        }
    }
}
