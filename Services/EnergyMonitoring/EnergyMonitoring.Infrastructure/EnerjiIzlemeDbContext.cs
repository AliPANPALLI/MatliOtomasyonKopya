using EnergyMonitoring.Domain.DbViewModels;
using EnergyMonitoring.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SharedApp.Domain.DbViewModels;

namespace EnergyMonitoring.Infrastructure
{
    public class EnerjiIzlemeDbContext : DbContext
    {
        private IConfiguration _configuration;

        public EnerjiIzlemeDbContext(DbContextOptions<DbContext> options)
      : base(options)
        {
        }

        public EnerjiIzlemeDbContext(IConfiguration configuration)
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
                optionsBuilder.UseSqlServer(_configuration.GetConnectionString("EnerjiIzlemeSqlServer"));
            }
        }

        public DbSet<AlarmLog> AlarmLog { get; set; }
        public DbSet<Bolum> Bolum { get; set; }
        public DbSet<Cihaz> Cihaz { get; set; }
        public DbSet<CihazModel> CihazModel { get; set; }
        public DbSet<EnerjiMaliyetParametreAtama> EnerjiMaliyetParametreAtama { get; set; }
        public DbSet<EnerjiMaliyetGrup> EnerjiMaliyetGrup { get; set; }
        public DbSet<EnerjiMaliyetParametreler> EnerjiMaliyetParametreler { get; set; }
        public DbSet<Gateway> Gateways { get; set; }
        public DbSet<Makine> Makine { get; set; }
        public DbSet<vwBolum> vwBolum { get; set; }
        public DbSet<vwCihaz> vwCihaz { get; set; }
        public DbSet<vwCihaz_Onay> vwCihaz_Onay { get; set; }
        public DbSet<vwMakine> vwMakine { get; set; }
        public DbSet<vwAlarmLog> vwAlarmLog { get; set; }
        public DbSet<vwLokasyon> vwLokasyon { get; set; }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<AlarmLog>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.KayitZamani).HasColumnType("datetime");
                entity.Property(e => e.Mesaj).HasColumnType("varchar(MAX)");
            });

            builder.Entity<Bolum>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.Name).HasColumnType("varchar(100)");
                entity.Property(e => e.OnaylanmaZamani).HasColumnType("datetime");
                entity.Property(e => e.OnaylayanUserId).HasColumnType("nvarchar(450)");
                entity.Property(e => e.EklenmeZamani).HasColumnType("datetime");
                entity.Property(e => e.EkleyenUserId).HasColumnType("nvarchar(450)");
                entity.Property(e => e.GuncellemeZamani).HasColumnType("datetime");
                entity.Property(e => e.GuncelleyenUserId).HasColumnType("nvarchar(450)");
            });

            builder.Entity<Cihaz>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.Name).HasColumnType("varchar(100)");
                entity.Property(e => e.KayitZamani).HasColumnType("datetime");
                entity.Property(e => e.SilinmeZamani).HasColumnType("datetime");
                entity.Property(e => e.OnaylanmaZamani).HasColumnType("datetime");
                entity.Property(e => e.OnaylayanUserId).HasColumnType("nvarchar(450)");
                entity.Property(e => e.EklenmeZamani).HasColumnType("datetime");
                entity.Property(e => e.EkleyenUserId).HasColumnType("nvarchar(450)");
                entity.Property(e => e.GuncellemeZamani).HasColumnType("datetime");
                entity.Property(e => e.GuncelleyenUserId).HasColumnType("nvarchar(450)");
                entity.HasOne(c => c.Makine)
                .WithMany(m => m.Cihazlar)
                .HasForeignKey(c => c.MakineId)
                .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(c => c.CihazModel)
                      .WithMany(m => m.Cihazlar)
                      .HasForeignKey(c => c.ModelId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(c => c.Gateway)
                      .WithMany(g => g.Cihazlar)
                      .HasForeignKey(c => c.GatewayId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<CihazModel>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.Marka).HasColumnType("varchar(100)");
                entity.Property(e => e.Model).HasColumnType("varchar(100)");
            });

            builder.Entity<EnerjiMaliyetGrup>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.Name).HasColumnType("varchar(250)");
            });

            builder.Entity<EnerjiMaliyetParametreAtama>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.NodeType).HasColumnType("varchar(50)");
                entity.Property(e => e.KayitZamani).HasColumnType("datetime");
            });

            builder.Entity<EnerjiMaliyetParametreler>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.Yekdem).HasColumnType("decimal(18,6)");
                entity.Property(e => e.DagitimBedeli).HasColumnType("decimal(18,6)");
                entity.Property(e => e.AnlasmaOrani).HasColumnType("decimal(18,6)");
                entity.Property(e => e.AktifEnerjiBedeli).HasColumnType("decimal(18,6)");
                entity.Property(e => e.GucBedeli).HasColumnType("decimal(18,6)");
                entity.Property(e => e.Gunduz).HasColumnType("decimal(18,6)");
                entity.Property(e => e.Puant).HasColumnType("decimal(18,6)");
                entity.Property(e => e.Gece).HasColumnType("decimal(18,6)");
            });

            builder.Entity<Gateway>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.Name).HasColumnType("varchar(100)");
                entity.Property(e => e.Marka).HasColumnType("varchar(100)");
                entity.Property(e => e.Model).HasColumnType("varchar(100)");
                entity.Property(e => e.IpAdres).HasColumnType("varchar(20)");
                entity.Property(e => e.Port).HasColumnType("varchar(20)");
                entity.Property(e => e.SubnetMask).HasColumnType("varchar(20)");
                entity.Property(e => e.RouterIp).HasColumnType("varchar(20)");
            });

            builder.Entity<Makine>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.Name).HasColumnType("varchar(100)");
                entity.Property(e => e.MakineNo).HasColumnType("varchar(50)");
                entity.Property(e => e.Aciklama).HasColumnType("varchar(100)");
                entity.Property(e => e.OnaylanmaZamani).HasColumnType("datetime");
                entity.Property(e => e.OnaylayanUserId).HasColumnType("nvarchar(450)");
                entity.Property(e => e.EklenmeZamani).HasColumnType("datetime");
                entity.Property(e => e.EkleyenUserId).HasColumnType("nvarchar(450)");
                entity.Property(e => e.GuncellemeZamani).HasColumnType("datetime");
                entity.Property(e => e.GuncelleyenUserId).HasColumnType("nvarchar(450)");
                entity.HasOne(c => c.Bolum)
                     .WithMany(m => m.Makineler)
                     .HasForeignKey(c => c.BolumId)
                     .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<vwAlarmLog>().HasNoKey().ToView("vwAlarmLog");
            builder.Entity<vwBolum>().HasNoKey().ToView("vwBolum");
            builder.Entity<vwCihaz>().HasNoKey().ToView("vwCihaz");
            builder.Entity<vwCihaz_Onay>().HasNoKey().ToView("vwCihaz_Onay");
            builder.Entity<vwMakine>().HasNoKey().ToView("vwMakine");
            builder.Entity<vwKumesEnerjiLog>().HasNoKey().ToView("vwKumesEnerjiLog");
            builder.Entity<vwLokasyon>().HasNoKey().ToView("vwLokasyon");
        }
    }
}