using Microsoft.EntityFrameworkCore;
using ProjeTakip.Domain.Entities;

namespace ProjeTakip.Infrastructure.Persistence;

public sealed class ProjeTakipDbContext(DbContextOptions<ProjeTakipDbContext> options) : DbContext(options)
{
    public DbSet<Sirket> Sirketler => Set<Sirket>();
    public DbSet<Tesis> Tesisler => Set<Tesis>();
    public DbSet<AdimBasligi> AdimBasliklari => Set<AdimBasligi>();
    public DbSet<Sorumlu> Sorumlular => Set<Sorumlu>();
    public DbSet<IsAdimi> IsAdimlari => Set<IsAdimi>();
    public DbSet<IsAdimiSorumlu> IsAdimiSorumlular => Set<IsAdimiSorumlu>();
    public DbSet<ProjeDokumani> ProjeDokumanlari => Set<ProjeDokumani>();
    public DbSet<IcmalKalemi> IcmalKalemleri => Set<IcmalKalemi>();
    public DbSet<ProjeTakipYetkisi> ProjeTakipYetkileri => Set<ProjeTakipYetkisi>();
    public DbSet<ProjeTakipDenetimKaydi> ProjeTakipDenetimKayitlari => Set<ProjeTakipDenetimKaydi>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Sirket>(entity =>
        {
            entity.ToTable("Sirket", "dbo");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Ad).HasMaxLength(150).IsRequired();
            entity.Property(x => x.OlusturulmaUtc).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property(x => x.GuncellemeUtc).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property(x => x.RowVersion).IsRowVersion();
            entity.HasIndex(x => x.Ad).IsUnique();
        });

        modelBuilder.Entity<Tesis>(entity =>
        {
            entity.ToTable("Tesis", "dbo");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Ad).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Slug).HasMaxLength(110).IsRequired();
            entity.Property(x => x.IlKodu);
            entity.Property(x => x.OlusturulmaUtc).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property(x => x.GuncellemeUtc).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property(x => x.RowVersion).IsRowVersion();
            entity.HasIndex(x => x.Ad).IsUnique();
            entity.HasIndex(x => x.Slug).IsUnique();
            entity.HasIndex(x => x.IlKodu);
            entity.HasIndex(x => x.SirketId);
            entity.HasOne(x => x.Sirket).WithMany(x => x.Tesisler).HasForeignKey(x => x.SirketId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AdimBasligi>(entity =>
        {
            entity.ToTable("AdimBasligi", "dbo");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Ad).HasMaxLength(255).IsRequired();
            entity.Property(x => x.OlusturulmaUtc).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property(x => x.GuncellemeUtc).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property(x => x.RowVersion).IsRowVersion();
            entity.HasIndex(x => x.Ad).IsUnique();
        });

        modelBuilder.Entity<Sorumlu>(entity =>
        {
            entity.ToTable("Sorumlu", "dbo");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.AdSoyad).HasMaxLength(200).IsRequired();
            entity.Property(x => x.HariciKullaniciId).HasMaxLength(128);
            entity.Property(x => x.RowVersion).IsRowVersion();
            entity.HasIndex(x => x.AdSoyad).IsUnique();
        });

        modelBuilder.Entity<IsAdimi>(entity =>
        {
            entity.ToTable("IsAdimi", "dbo", table => table.UseSqlOutputClause(false));
            entity.HasKey(x => x.Id);
            entity.Property(x => x.IsNo).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Ad).HasMaxLength(255).IsRequired();
            entity.Property(x => x.Durum).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Oncelik).HasMaxLength(10).IsRequired();
            entity.Property(x => x.Baslangic).HasColumnType("date");
            entity.Property(x => x.HedefBitis).HasColumnType("date");
            entity.Property(x => x.Notlar).IsRequired();
            entity.Property(x => x.Risk).IsRequired();
            entity.Property(x => x.SonrakiAdim).HasMaxLength(255).IsRequired();
            entity.Property(x => x.OlusturulmaUtc).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property(x => x.GuncellemeUtc).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property(x => x.RowVersion).IsRowVersion();
            entity.HasIndex(x => x.IsNo).IsUnique();
            entity.HasOne(x => x.Tesis).WithMany(x => x.IsAdimlari).HasForeignKey(x => x.TesisId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.UstAdim).WithMany(x => x.AltAdimlar).HasForeignKey(x => x.UstId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<IsAdimiSorumlu>(entity =>
        {
            entity.ToTable("IsAdimiSorumlu", "dbo");
            entity.HasKey(x => new { x.IsAdimiId, x.SorumluId });
            entity.Property(x => x.AtanmaUtc).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasOne(x => x.IsAdimi).WithMany(x => x.IsAdimiSorumlular).HasForeignKey(x => x.IsAdimiId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Sorumlu).WithMany(x => x.IsAdimiSorumlular).HasForeignKey(x => x.SorumluId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProjeDokumani>(entity =>
        {
            entity.ToTable("ProjeDokumani", "dbo");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Tur).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Ad).HasMaxLength(255).IsRequired();
            entity.Property(x => x.BelgeNo).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Revizyon).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Durum).HasMaxLength(20).IsRequired();
            entity.Property(x => x.BelgeTarihi).HasColumnType("date");
            entity.Property(x => x.GecerlilikTarihi).HasColumnType("date");
            entity.Property(x => x.Tutar).HasColumnType("decimal(18,2)");
            entity.Property(x => x.Aciklama).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.DosyaAdi).HasMaxLength(255);
            entity.Property(x => x.IcerikTipi).HasMaxLength(150);
            entity.Property(x => x.OlusturulmaUtc).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property(x => x.RowVersion).IsRowVersion();
            entity.HasIndex(x => new { x.ProjeId, x.Tur });
            entity.HasOne(x => x.Proje).WithMany(x => x.Dokumanlar).HasForeignKey(x => x.ProjeId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<IcmalKalemi>(entity =>
        {
            entity.ToTable("IcmalKalemi", "dbo");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.PozNo).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Tanim).HasMaxLength(500).IsRequired();
            entity.Property(x => x.Birim).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Miktar).HasColumnType("decimal(18,3)");
            entity.Property(x => x.BirimFiyat).HasColumnType("decimal(18,4)");
            entity.Property(x => x.ParaBirimi).HasMaxLength(3).IsRequired();
            entity.Property(x => x.Tutar).HasColumnType("decimal(18,2)");
            entity.Property(x => x.Aciklama).HasMaxLength(500).IsRequired();
            entity.Property(x => x.OlusturulmaUtc).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property(x => x.RowVersion).IsRowVersion();
            entity.HasIndex(x => new { x.ProjeDokumaniId, x.SiraNo });
            entity.HasIndex(x => x.IsAdimiId);
            entity.HasOne(x => x.ProjeDokumani).WithMany(x => x.IcmalKalemleri)
                .HasForeignKey(x => x.ProjeDokumaniId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.IsAdimi).WithMany(x => x.IcmalKalemleri)
                .HasForeignKey(x => x.IsAdimiId).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<ProjeTakipYetkisi>(entity =>
        {
            entity.ToTable("ProjeTakipYetkisi", "dbo");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.KullaniciId).HasMaxLength(128).IsRequired();
            entity.Property(x => x.KullaniciAdi).HasMaxLength(200).IsRequired();
            entity.Property(x => x.OlusturulmaUtc).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property(x => x.RowVersion).IsRowVersion();
            entity.HasIndex(x => x.KullaniciId).IsUnique();
        });

        modelBuilder.Entity<ProjeTakipDenetimKaydi>(entity =>
        {
            entity.ToTable("ProjeTakipDenetimKaydi", "dbo");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.KullaniciId).HasMaxLength(128).IsRequired();
            entity.Property(x => x.KullaniciAdi).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Islem).HasMaxLength(50).IsRequired();
            entity.Property(x => x.KayitTuru).HasMaxLength(80).IsRequired();
            entity.Property(x => x.KayitId).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Aciklama).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.IpAdresi).HasMaxLength(64).IsRequired();
            entity.Property(x => x.IslemUtc).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasIndex(x => x.IslemUtc);
            entity.HasIndex(x => new { x.KayitTuru, x.KayitId });
        });
    }
}
