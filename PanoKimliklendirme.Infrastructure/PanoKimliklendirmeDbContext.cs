using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PanoKimliklendirme.Domain.DbViewModels;
using PanoKimliklendirme.Domain.Models;
using SharedApp.Domain.DbViewModels;

namespace PanoKimliklendirme.Infrastructure
{
    public class PanoKimliklendirmeDbContext : DbContext
    {
        private IConfiguration _configuration;

        public PanoKimliklendirmeDbContext(DbContextOptions<DbContext> options)
      : base(options)
        {
        }

        public PanoKimliklendirmeDbContext(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(_configuration.GetConnectionString("SqlServer"));
        }
        public DbSet<Bolum> Bolum { get; set; }
        public DbSet<Cihaz> Cihaz { get; set; }
        public DbSet<LokasyonDosyaYoluEsleme> LokasyonDosyaYoluEsleme { get; set; }
        public DbSet<Makine> Makine { get; set; }
        public DbSet<OnayIslemKayit> OnayIslemKayit { get; set; }
        public DbSet<Pano> Pano { get; set; }
        public DbSet<PanoMakineEsleme> PanoMakineEsleme { get; set; }
        public DbSet<vwDistinctOnayIslemKayit> vwDistinctOnayIslemKayit { get; set; }
        public DbSet<vwOnayIslemKayit> vwOnayIslemKayit { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<Bolum>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(50);
                entity.Property(e => e.LokasyonId).IsRequired();
                entity.Property(e => e.ParentBolumId).IsRequired();
            });

            builder.Entity<Cihaz>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.Name).HasMaxLength(50);
                entity.Property(e => e.Marka).HasMaxLength(50);
                entity.Property(e => e.UrunKodu).HasMaxLength(50);
                entity.Property(e => e.IP).HasMaxLength(30);
                entity.Property(e => e.PanoId).IsRequired();
            });

            builder.Entity<LokasyonDosyaYoluEsleme>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.DosyaYolu).HasColumnType("varchar(500)");
            });

            builder.Entity<Makine>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(50);
                entity.Property(e => e.MakineNo).HasMaxLength(50);
                entity.Property(e => e.BolumId).IsRequired();
                entity.Property(e => e.ParentMakineId).IsRequired();
                entity.Property(e => e.Aciklama).HasMaxLength(150);

                entity.HasOne(p => p.Bolum) // Makine sınıfındaki Bolum özelliğiyle ilişkilendir
                .WithMany(b => b.Makines) // Bolum sınıfındaki Makines koleksiyonu ile ilişkilendir
                .HasForeignKey(p => p.BolumId); // Makine tablosundaki BolumId sütunu ile dış anahtar ilişkisi kur
            });

            builder.Entity<OnayIslemKayit>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UserId).HasColumnType("nvarchar(450)");
                entity.Property(e => e.OgeTipi).HasColumnType("varchar(10)");
                entity.Property(e => e.IslemVerisi).HasColumnType("varchar(MAX)");
                entity.Property(e => e.KayitZamani).HasColumnType("datetime");
                entity.Property(e => e.GuncellemeZamani).HasColumnType("datetime");
                entity.Property(e => e.GuncelleyenUserId).HasColumnType("nvarchar(450)");
                entity.Property(e => e.DosyaAd).HasColumnType("varchar(500)");
                entity.Property(e => e.KlasorAd).HasColumnType("varchar(20)");
            });

            builder.Entity<Pano>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(50).HasColumnType("varchar(50)");
                entity.Property(e => e.PanoKimlikNo).HasMaxLength(100).HasColumnType("varchar(100)");
                entity.HasIndex(a => a.PanoKimlikNo).IsUnique(); //PanoKimlikNo tekrarlanmamasi gerektiği
                entity.Property(e => e.ParentPanoId).IsRequired();
                entity.Property(e => e.DosyaYolu).HasMaxLength(500).HasColumnType("varchar(500)");
            });

            builder.Entity<PanoMakineEsleme>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.HasOne(p => p.Makine)
               .WithMany(b => b.PanoMakineEslemes)
               .HasForeignKey(p => p.MakineId); // Makine tablosundaki Id ile MakineId sütunu arasında dış anahtar ilişkisi kur

                entity.HasOne(p => p.Pano)
               .WithMany(b => b.PanoMakineEslemes)
               .HasForeignKey(p => p.PanoId); // Pano tablosundaki Id ile PanoId sütunu arasında dış anahtar ilişkisi kur
            });

            builder.Entity<vwDistinctOnayIslemKayit>().HasNoKey().ToView("vwDistinctOnayIslemKayit");
            builder.Entity<vwOnayIslemKayit>().HasNoKey().ToView("vwOnayIslemKayit");

            base.OnModelCreating(builder);
        }
    }
}