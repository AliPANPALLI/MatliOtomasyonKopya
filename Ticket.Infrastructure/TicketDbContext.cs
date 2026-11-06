using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Ticket.Domain.DbViewModels;
using Ticket.Domain.Models;

namespace Ticket.Infrastructure
{
    public class TicketDbContext : DbContext
    {
        private IConfiguration _configuration;
        public TicketDbContext(DbContextOptions<DbContext> options) : base(options)
        {
        }

        public TicketDbContext(IConfiguration configuration)
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
                optionsBuilder.UseSqlServer(_configuration.GetConnectionString("TicketSqlServer"));
            }
        }
        public DbSet<ParaBirimi> ParaBirimi { get; set; }
        public DbSet<TakipciUser> TakipciUser { get; set; }
        public DbSet<Tedarikci> Tedarikci { get; set; }
        public DbSet<TedarikciUser> TedarikciUser { get; set; }
        public DbSet<TedarikciUserEsleme> TedarikciUserEsleme { get; set; }
        public DbSet<TicketAciklamaEsleme> TicketAciklamaEsleme { get; set; }
        public DbSet<TicketDurum> TicketDurum { get; set; }
        public DbSet<TicketDurumLog> TicketDurumLog { get; set; }
        public DbSet<TicketTeklifFiyatLog> TicketTeklifFiyatLog { get; set; }
        public DbSet<TicketLogs> TicketLogs { get; set; }
        public DbSet<TicketMatliTakipciEsleme> TicketMatliTakipciEsleme { get; set; }
        public DbSet<TicketSorumluUserEsleme> TicketSorumluUserEsleme { get; set; }
        public DbSet<TicketTanim> TicketTanim { get; set; }
        public DbSet<vwTicketMatliTakipciEsleme> vwTicketMatliTakipciEsleme { get; set; }
        public DbSet<vwTicketSorumluUserEsleme> vwTicketSorumluUserEsleme { get; set; }
        public DbSet<vwTicketTanim> vwTicketTanim { get; set; }
        public DbSet<vwTedarikciUser> vwTedarikciUser { get; set; }
        public DbSet<vwTicketAciklamaEsleme> vwTicketAciklamaEsleme { get; set; }
        public DbSet<vwTicketDurumLog> vwTicketDurumLog { get; set; }
        public DbSet<vwTicketTeklifFiyatLog> vwTicketTeklifFiyatLog { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {

            builder.Entity<ParaBirimi>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.Birim).HasColumnType("varchar(10)");
            });

            builder.Entity<TakipciUser>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.LoginUserId).HasColumnType("nvarchar(450)");
            });

            builder.Entity<Tedarikci>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.Name).HasColumnType("varchar(100)");
            });

            builder.Entity<TedarikciUser>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.Name).HasColumnType("varchar(50)");
                entity.Property(e => e.Surname).HasColumnType("varchar(100)");
                entity.Property(e => e.PhoneNumber).HasColumnType("varchar(20)");
                entity.Property(e => e.Email).HasColumnType("varchar(100)");
                entity.Property(e => e.LoginUserId).HasColumnType("nvarchar(450)");
            });

            builder.Entity<TicketAciklamaEsleme>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.Aciklama).HasColumnType("varchar(MAX)");
                entity.Property(e => e.KayitZamani).HasColumnType("datetime");
            });

            builder.Entity<TicketDurum>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.Name).HasColumnType("varchar(50)");
                entity.Property(e => e.Aciklama).HasColumnType("varchar(100)");
                entity.Property(e => e.BackColor).HasColumnType("varchar(20)");
                entity.Property(e => e.TextColor).HasColumnType("varchar(20)");
            });

            builder.Entity<TicketDurumLog>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.KayitZamani).HasColumnType("datetime");
            });

            builder.Entity<TicketTeklifFiyatLog>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.TeklifFiyatOncesi).HasColumnType("decimal(15,3)");
                entity.Property(e => e.TeklifFiyatSonrasi).HasColumnType("decimal(15,3)");
                entity.Property(e => e.MatliUserId).HasColumnType("nvarchar(450)");
                entity.Property(e => e.KayitZamani).HasColumnType("datetime");
            });

            builder.Entity<TicketLogs>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.KayitZamani).HasColumnType("datetime");
                entity.Property(e => e.MatliUserId).HasColumnType("nvarchar(450)");
                entity.Property(e => e.YapilanDegisiklik).HasColumnType("varchar(MAX)");
            });

            builder.Entity<TicketMatliTakipciEsleme>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.MatliUserId).HasColumnType("nvarchar(450)");
            });

            builder.Entity<TicketSorumluUserEsleme>(entity =>
            {
                entity.HasKey(x => x.Id);
            });

            builder.Entity<TicketTanim>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.OlusturanUserId).HasColumnType("nvarchar(450)");
                entity.Property(e => e.Tanim).HasColumnType("varchar(MAX)");
                entity.Property(e => e.KisaTanim).HasColumnType("varchar(30)");
                entity.Property(e => e.KayitZamani).HasColumnType("datetime");
                entity.Property(e => e.TeklifFiyat).HasColumnType("decimal(15,3)");
                entity.Property(e => e.GuncellemeZamani).HasColumnType("datetime");
                entity.Property(e => e.GuncelleyenUserId).HasColumnType("nvarchar(450)");
                entity.Property(e => e.GuncellemedeYapilanIslem).HasColumnType("varchar(50)");
                entity.Property(e => e.SasNo).HasColumnType("varchar(50)");
                entity.Property(e => e.TeklifKararDurumGuncellemeZamani).HasColumnType("datetime");
                entity.Property(e => e.TeklifKararDurumGuncelleyenUserId).HasColumnType("nvarchar(450)");
            });

            builder.Entity<vwTicketMatliTakipciEsleme>().HasNoKey().ToView("vwTicketMatliTakipciEsleme");
            builder.Entity<vwTicketSorumluUserEsleme>().HasNoKey().ToView("vwTicketSorumluUserEsleme");
            builder.Entity<vwTicketTanim>().HasNoKey().ToView("vwTicketTanim");
            builder.Entity<vwTedarikciUser>().HasNoKey().ToView("vwTedarikciUser");
            builder.Entity<vwTicketAciklamaEsleme>().HasNoKey().ToView("vwTicketAciklamaEsleme");
            builder.Entity<vwTicketDurumLog>().HasNoKey().ToView("vwTicketDurumLog");
            builder.Entity<vwTicketTeklifFiyatLog>().HasNoKey().ToView("vwTicketTeklifFiyatLog");

            base.OnModelCreating(builder);
        }
    }
}