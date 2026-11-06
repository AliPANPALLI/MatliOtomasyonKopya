
using KantarTakip.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace KantarTakip.Infrastructure
{
    public class KantarTakipDbContext : DbContext
    {
        public KantarTakipDbContext(
            DbContextOptions<KantarTakipDbContext> options)
            : base(options)
        {
        }

        public DbSet<Firma> Firma { get; set; }
        public DbSet<KaliteKontrolPersonel> KaliteKontrolPersonel { get; set; }
        public DbSet<KantarGuvenlik> KantarGuvenlik { get; set; }
        public DbSet<KantarKalite> KantarKalite { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            #region Firma

            builder.Entity<Firma>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(e => e.Name)
                    .HasColumnType("varchar(300)");
            });

            #endregion

            #region KaliteKontrolPersonel

            builder.Entity<KaliteKontrolPersonel>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(e => e.AdSoyad)
                    .HasColumnType("nvarchar(200)");

                entity.Property(e => e.Silindi)
                    .HasColumnType("bit");
            });

            #endregion

            #region KantarGuvenlik

            builder.Entity<KantarGuvenlik>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(e => e.Plaka)
                    .HasColumnType("varchar(100)");

                entity.Property(e => e.PlakaNormalized)
                    .HasColumnType("varchar(100)");

                entity.HasIndex(e => e.PlakaNormalized);

                entity.Property(e => e.KayitZamani)
                    .HasColumnType("datetime");

                entity.HasIndex(e => e.KayitZamani);

                entity.Property(e => e.Tarih)
                    .HasColumnType("date")
                    .IsRequired();

                entity.Property(e => e.UrunAdi)
                    .HasColumnType("nvarchar(200)");

                entity.Property(e => e.IrsaliyeNo)
                    .HasColumnType("nvarchar(50)");

                entity.Property(e => e.SonUrun)
                    .HasColumnType("nvarchar(200)");

                entity.Property(e => e.ZiyaretciKayitId)
                    .HasColumnType("nvarchar(50)");

                // Silinmemiş kayıtlar arasında benzersiz.
                entity.HasIndex(e => e.ZiyaretciKayitId)
                    .IsUnique()
                    .HasFilter("[ZiyaretciKayitId] IS NOT NULL AND [Silindi] = 0");

                entity.Property(e => e.KaliteSonucu)
                    .HasConversion<int?>();

                entity.Property(e => e.DokulduguYer)
                    .HasColumnType("nvarchar(200)");

                entity.Property(e => e.BosaltimZamani)
                    .HasColumnType("datetime");

                entity.Property(e => e.BosaltimYapanUserId)
                    .HasColumnType("nvarchar(450)");

                entity.Property(e => e.GuncelleyenUserId)
                    .HasColumnType("nvarchar(450)");

                entity.Property(e => e.Silindi)
                    .HasColumnType("bit");
                entity.Property(x => x.KaliteTamamlandi)
                .HasDefaultValue(false)
                .IsRequired();

                entity.Property(x => x.KaliteTamamlanmaZamani);

                entity.Property(x => x.KaliteTamamlayanUserId)
                    .HasMaxLength(450);
                entity.HasOne(x => x.Firma)
                    .WithMany(f => f.KantarGuvenlikler)
                    .HasForeignKey(x => x.FirmaId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            #endregion

            #region KantarKalite

            builder.Entity<KantarKalite>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.Nem).HasColumnType("decimal(10,3)");
                entity.Property(x => x.Protein).HasColumnType("decimal(10,3)");
                entity.Property(x => x.Kul).HasColumnType("decimal(10,3)");
                entity.Property(x => x.Seluloz).HasColumnType("decimal(10,3)");
                entity.Property(x => x.Yag).HasColumnType("decimal(10,3)");
                entity.Property(x => x.Nisasta).HasColumnType("decimal(10,3)");
                entity.Property(x => x.Koh).HasColumnType("decimal(10,3)");
                entity.Property(x => x.Ureaz).HasColumnType("decimal(10,3)");

                entity.Property(x => x.Koku).HasColumnType("nvarchar(100)");
                entity.Property(x => x.Tozluluk).HasColumnType("nvarchar(100)");
                entity.Property(x => x.Curuk).HasColumnType("nvarchar(100)");
                entity.Property(x => x.Kuf).HasColumnType("nvarchar(100)");
                entity.Property(x => x.Boceklenme).HasColumnType("nvarchar(100)");

                entity.Property(x => x.Sonuc).HasConversion<int?>();
                entity.Property(x => x.SonucAciklama).HasColumnType("nvarchar(500)");

                entity.Property(e => e.GuncelleyenUserId)
                    .HasColumnType("nvarchar(450)");

                entity.Property(e => e.KayitZamani)
                    .HasColumnType("datetime");

                entity.Property(e => e.Silindi)
                    .HasColumnType("bit");

                entity.HasOne(x => x.KantarGuvenlik)
                    .WithMany(k => k.KantarKaliteler)
                    .HasForeignKey(x => x.KantarGuvenlikId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.KaliteKontrolPersonel)
                    .WithMany(p => p.KantarKaliteler)
                    .HasForeignKey(x => x.KaliteKontrolPersonelId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            #endregion
        }
    }
}
