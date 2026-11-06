using Authentication.Domain.DbViewModels;
using Authentication.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Authentication.Infrastructure
{
    public class AuthDbContext : DbContext
    {
        private IConfiguration _configuration;

        public AuthDbContext(DbContextOptions<DbContext> options)
      : base(options)
        {
        }

        public AuthDbContext(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(_configuration.GetConnectionString("SqlServer"));
        }
        public DbSet<ConnectSiteLog> ConnectSiteLog { get; set; }
        public DbSet<OneTimePassword> OneTimePassword { get; set; }
        public DbSet<PageVisit> PageVisit { get; set; }
        public DbSet<PageVisitMain> PageVisitMain { get; set; }
        public DbSet<User> User { get; set; }
        public DbSet<UserHeartBeat> UserHeartBeat { get; set; }
        public DbSet<UserHeartBeatMain> UserHeartBeatMain { get; set; }
        public DbSet<UserRefreshToken> UserRefreshTokens { get; set; }
        public DbSet<vwConnectSiteLog> vwConnectSiteLog { get; set; }
        public DbSet<vwPageVisit> vwPageVisit { get; set; }
        public DbSet<vwPageVisitMain> vwPageVisitMain { get; set; }
        public DbSet<vwUser> vwUser { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<ConnectSiteLog>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.LocalName).HasColumnType("varchar(100)");
                entity.Property(e => e.HostName).HasColumnType("varchar(100)");
                entity.Property(e => e.Browser).HasColumnType("varchar(100)");
                entity.Property(e => e.BrowserVersion).HasColumnType("varchar(100)");
                entity.Property(e => e.NetIp).HasColumnType("varchar(100)");
                entity.Property(e => e.LocalIp).HasColumnType("varchar(100)");
                entity.Property(e => e.Page).HasColumnType("varchar(100)");
                entity.Property(e => e.Tarih).HasColumnType("datetime");
                entity.Property(e => e.UserId).HasColumnType("nvarchar(450)");
                entity.Property(e => e.Aciklama).HasColumnType("varchar(500)");
            });

            builder.Entity<OneTimePassword>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.EklenmeTarihi).HasColumnType("datetime");
                entity.Property(e => e.EkleyenUserId).HasColumnType("varchar(500)");
                entity.Property(e => e.GuncellemeTarihi).HasColumnType("datetime");
                entity.Property(e => e.GuncelleyenUserId).HasColumnType("varchar(500)");
                entity.Property(e => e.OtpSecretCode).HasColumnType("varchar(MAX)");//TODO:50'ye cekilecek
                entity.Property(e => e.OtpCode).HasColumnType("varchar(50)");
                entity.Property(e => e.UserId).HasColumnType("varchar(500)");
                entity.Property(e => e.Aktif).HasColumnType("bit");
                entity.Property(e => e.Kullanildi).HasColumnType("bit");
                entity.Property(e => e.IP).HasColumnType("varchar(30)");
                entity.Property(e => e.Domain).HasColumnType("bit");
            });

            builder.Entity<PageVisit>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.UserId).HasColumnType("nvarchar(450)");
                entity.Property(e => e.TabId).HasColumnType("nvarchar(450)");
                entity.Property(e => e.PageName).HasColumnType("varchar(500)");
                entity.Property(e => e.ModulLink).HasColumnType("varchar(300)");
                entity.Property(e => e.FormLink).HasColumnType("varchar(300)");
                entity.Property(e => e.UrlId).HasColumnType("varchar(50)");
                entity.Property(e => e.ZiyaretZamani).HasColumnType("datetime");
                entity.Property(e => e.CikisZamani).HasColumnType("datetime");
            });

            builder.Entity<PageVisitMain>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.UserId).HasColumnType("nvarchar(450)");
                entity.Property(e => e.TabId).HasColumnType("nvarchar(450)");
                entity.Property(e => e.PageName).HasColumnType("varchar(500)");
                entity.Property(e => e.ModulLink).HasColumnType("varchar(300)");
                entity.Property(e => e.FormLink).HasColumnType("varchar(300)");
                entity.Property(e => e.UrlId).HasColumnType("varchar(50)");
                entity.Property(e => e.ZiyaretZamani).HasColumnType("datetime");
                entity.Property(e => e.CikisZamani).HasColumnType("datetime");
            });

            builder.Entity<User>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.Name).HasMaxLength(50);
                entity.Property(e => e.Position).HasMaxLength(50);
                entity.Property(e => e.SicilNo).HasMaxLength(50);
            });

            builder.Entity<UserHeartBeat>(entity =>
            {
                entity.HasKey(x => x.UserId);
                entity.Property(e => e.UserId).HasColumnType("nvarchar(450)");
                entity.Property(e => e.LastSeen).HasColumnType("datetime");

            });

            builder.Entity<UserHeartBeatMain>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.UserId).HasColumnType("nvarchar(450)");
                entity.Property(e => e.PageName).HasColumnType("varchar(500)");
                entity.Property(e => e.ModulLink).HasColumnType("varchar(300)");
                entity.Property(e => e.FormLink).HasColumnType("varchar(300)");
                entity.Property(e => e.UrlId).HasColumnType("varchar(50)");
                entity.Property(e => e.TabId).HasColumnType("nvarchar(450)");
                entity.Property(e => e.LastSeen).HasColumnType("datetime");
            });

            builder.Entity<UserRefreshToken>(entity =>
            {
                entity.HasKey(x => x.UserId);
                entity.Property(e => e.Code).HasColumnType("varchar(500)");
                entity.Property(e => e.Expiration).HasColumnType("datetime");
            });

            builder.Entity<vwUser>().HasNoKey().ToView("vwUser");
            builder.Entity<vwConnectSiteLog>().HasNoKey().ToView("vwConnectSiteLog");
            builder.Entity<vwPageVisit>().HasNoKey().ToView("vwPageVisit");
            builder.Entity<vwPageVisitMain>().HasNoKey().ToView("vwPageVisitMain");

            base.OnModelCreating(builder);
        }
    }
}
