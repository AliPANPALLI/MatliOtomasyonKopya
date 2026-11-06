using MakineIzleme.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace MakineIzleme.Infrastructure
{
    public class MakineIzlemeDbContext : DbContext
    {
        private IConfiguration _configuration;
        public MakineIzlemeDbContext(DbContextOptions<DbContext> options) : base(options)
        {
        }

        public MakineIzlemeDbContext(IConfiguration configuration)
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
                optionsBuilder.UseSqlServer(_configuration.GetConnectionString("MakineIzlemeSqlServer"));
            }
        }

        public DbSet<Bolum> Bolum { get; set; }
        public DbSet<Makine> Makine { get; set; }
        public DbSet<MakineLog> MakineLog { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Bolum>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.Name).HasColumnType("varchar(50)");
            });

            builder.Entity<Makine>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.Name).HasColumnType("varchar(100)");
                entity.HasOne(c => c.Bolum).WithMany(m => m.Makineler).HasForeignKey(c => c.BolumId).OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<MakineLog>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.KayitZamani).HasColumnType("datetime");
                entity.Property(e => e.Aciklama).HasColumnType("varchar(250)");
            });
        }
    }
}