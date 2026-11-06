using Microsoft.EntityFrameworkCore;
using MobileUpdate.Application.Interfaces;
using MobileUpdate.Domain.Entities;

namespace MobileUpdate.Infrastructure
{
    /// <summary>
    /// mobil_guncelleme PostgreSQL veritabanı. İki tablo yeterli: yayınlanan sürümler ve
    /// cihazların kurulu sürümleri. APK dosyası burada değil diskte tutulur.
    /// </summary>
    public class MobilGuncellemeDbContext : DbContext, IMobilGuncellemeDbContext
    {
        public MobilGuncellemeDbContext(DbContextOptions<MobilGuncellemeDbContext> options) : base(options)
        {
        }

        public DbSet<UygulamaSurum> UygulamaSurum => Set<UygulamaSurum>();
        public DbSet<Cihaz> Cihaz => Set<Cihaz>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UygulamaSurum>(e =>
            {
                e.ToTable("UygulamaSurum");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).UseIdentityByDefaultColumn();

                // Karşılaştırma bu alanla yapılır; iki sürüm aynı koda sahip olamaz.
                e.HasIndex(x => x.VersionCode).IsUnique();

                e.Property(x => x.VersionName).HasMaxLength(20).IsRequired();
                e.Property(x => x.DosyaAdi).HasMaxLength(200).IsRequired();
                e.Property(x => x.Sha256).HasColumnType("char(64)").IsRequired();
                e.Property(x => x.SurumNotu).HasColumnType("text");
                e.Property(x => x.YuklenmeTarihi).HasColumnType("timestamptz");
                e.Property(x => x.Yukleyen).HasMaxLength(100);
            });

            modelBuilder.Entity<Cihaz>(e =>
            {
                e.ToTable("Cihaz");
                e.HasKey(x => x.CihazId);
                e.Property(x => x.CihazId).HasMaxLength(64);
                e.Property(x => x.KullaniciId).HasMaxLength(64);
                e.Property(x => x.CihazModeli).HasMaxLength(100);
                e.Property(x => x.AndroidSurumu).HasMaxLength(20);
                e.Property(x => x.SonGorulme).HasColumnType("timestamptz");
            });
        }
    }
}
