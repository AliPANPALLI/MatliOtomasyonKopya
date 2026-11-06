using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using EnvironmentMonitoringScada.Domain.Models;

namespace EnvironmentMonitoringScada.Infrastructure
{
    public class OrtamIzlemeScadaDbContext : DbContext
    {
        private IConfiguration _configuration;
        public OrtamIzlemeScadaDbContext(DbContextOptions<DbContext> options) : base(options)
        {
        }

        public OrtamIzlemeScadaDbContext(IConfiguration configuration)
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
                optionsBuilder.UseSqlServer(_configuration.GetConnectionString("OrtamIzlemeSqlServer"));
            }
        }

        public DbSet<Bolum> Bolum { get; set; }
        public DbSet<Cihaz> Cihaz { get; set; }
        public DbSet<CihazLog> CihazLog { get; set; }
        public DbSet<Oda> Oda { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Bolum>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.BolumAdi).HasColumnType("varchar(50)");
            });

            builder.Entity<Cihaz>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.Marka).HasColumnType("varchar(50)");
                entity.Property(e => e.Model).HasColumnType("varchar(50)");
                entity.Property(e => e.SensorAdi).HasColumnType("varchar(50)");
                entity.Property(e => e.KayitZamani).HasColumnType("datetime");
                entity.HasOne(c => c.Oda).WithMany(m => m.Cihazlar).HasForeignKey(c => c.OdaId).OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<CihazLog>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.KayitZamani).HasColumnType("datetime");
                entity.Property(e => e.SN_1_LastSeen).HasColumnType("datetime");
                entity.Property(e => e.SN_2_LastSeen).HasColumnType("datetime");
                entity.Property(e => e.SB_1_LastSeen).HasColumnType("datetime");
                entity.Property(e => e.SB_2_LastSeen).HasColumnType("datetime");
                entity.Property(e => e.HS_1_LastSeen).HasColumnType("datetime");
                entity.Property(e => e.KS_1_LastSeen).HasColumnType("datetime");
            });

            builder.Entity<Oda>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.OdaAdi).HasColumnType("varchar(50)");
                entity.HasOne(c => c.Bolum).WithMany(m => m.Odalar).HasForeignKey(c => c.BolumId).OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}