using Microsoft.EntityFrameworkCore;
using ObjectMonitoring.Domain.DbViewModels;
using ObjectMonitoring.Domain.Entities;
using ObjectMonitoring.Domain.Models.ReportModels;
using System.Reflection.Emit;

namespace ObjectMonitoring.Infrastructure;

public class NesneIzlemeDbContext : DbContext
{
    public NesneIzlemeDbContext(
        DbContextOptions<NesneIzlemeDbContext> options)
        : base(options)
    {
    }

    public DbSet<Debimetre> Debimetre
        => Set<Debimetre>();

    public DbSet<DebimetreAnlikDurum> DebimetreAnlikDurum
        => Set<DebimetreAnlikDurum>();

    public DbSet<DebimetreKategori> DebimetreKategori
        => Set<DebimetreKategori>();

    public DbSet<DebimetreOlcum> DebimetreOlcum
        => Set<DebimetreOlcum>();

    public DbSet<DebimetreOzet> DebimetreOzet
        => Set<DebimetreOzet>();

    public DbSet<Tbl_EtiketNesne> EtiketNesneleri
        => Set<Tbl_EtiketNesne>();

    public DbSet<Tbl_LogNesne> Tbl_LogNesne
        => Set<Tbl_LogNesne>();

    public DbSet<Tbl_MachineStateType> Tbl_MachineStateType
        => Set<Tbl_MachineStateType>();

    public DbSet<Tbl_Makine> Tbl_Makine
        => Set<Tbl_Makine>();

    public DbSet<Tbl_Nesne> Tbl_Nesne
        => Set<Tbl_Nesne>();

    public DbSet<Tbl_StateType> Tbl_StateType
        => Set<Tbl_StateType>();

    public DbSet<Tbl_ValueType> Tbl_ValueType
        => Set<Tbl_ValueType>();

    public DbSet<DebimetreRaporVeriModel> DebimetreRaporVerileri { get; set; }
    public DbSet<vwLokasyon> vwLokasyon { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        ValueTypeModelOlustur(builder);
        NesneModelOlustur(builder);
        LogNesneModelOlustur(builder);
        builder.Entity<DebimetreRaporVeriModel>(entity =>
        {
            entity.HasNoKey();
            entity.ToView(null);
        });

        builder.Entity<DebimetreAnlikDurum>(entity =>
        {
            entity.ToTable("DebimetreAnlikDurum", "dbo");

            entity.HasKey(x => x.DebimetreId)
                .HasName("PK_DebimetreAnlikDurum");

            entity.Property(x => x.DebimetreId)
                .ValueGeneratedNever();

            entity.Property(x => x.OkunmaZamani)
                .HasColumnType("datetime2(3)");

            entity.Property(x => x.SonBasariliOkunmaZamani)
                .HasColumnType("datetime2(3)");

            entity.Property(x => x.Debi)
                .HasPrecision(20, 6);

            entity.Property(x => x.PozitifToplam)
                .HasPrecision(20, 6);

            entity.Property(x => x.NegatifToplam)
                .HasPrecision(20, 6);

            entity.Property(x => x.TuketimTarihi)
                .HasColumnType("date");

            entity.Property(x => x.BugunTuketim)
                .HasPrecision(20, 6);

            entity.Property(x => x.Durum)
                .HasColumnType("tinyint")
                .HasDefaultValue((byte)0)
                .IsRequired();

            entity.Property(x => x.TuketimGecerli)
                .HasDefaultValue(false)
                .IsRequired();

            entity.Property(x => x.SayacSifirlandi)
                .HasDefaultValue(false)
                .IsRequired();

            entity.Property(x => x.GuncellemeZamani)
                .HasColumnType("datetime2(3)")
                .HasDefaultValueSql("sysdatetime()")
                .IsRequired();

            entity.Property(x => x.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.Property(x => x.DununSonPozitifToplam)
                .HasPrecision(20, 6);

            entity.HasOne(x => x.Debimetre)
                .WithOne(x => x.AnlikDurum)
                .HasForeignKey<DebimetreAnlikDurum>(x => x.DebimetreId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_DebimetreAnlikDurum_Debimetre");
        });
    }

    private static void ValueTypeModelOlustur(ModelBuilder builder)
    {
        builder.Entity<Tbl_ValueType>(entity =>
        {
            entity.ToTable("Tbl_ValueType", "dbo");

            entity.HasKey(x => x.Id)
                .HasName("PK_Tbl_ValueType");

            entity.Property(x => x.Id)
                .ValueGeneratedOnAdd();

            entity.Property(x => x.Name)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.Property(x => x.EnumDegeri)
                .HasColumnName("Enum")
                .IsRequired();

            entity.Property(x => x.Aciklama)
                .HasMaxLength(50)
                .IsUnicode(false);

            /*
             * Tbl_LogNesne tablosundaki Value1_Type,
             * Value2_Type ve Value3_Type alanları
             * Tbl_ValueType.Id alanına değil,
             * Tbl_ValueType.Enum alanına bağlı.
             */
            entity.HasAlternateKey(x => x.EnumDegeri)
                .HasName("AK_Tbl_ValueType_Enum");
        });
        builder.Entity<vwLokasyon>().HasNoKey().ToView("vwLokasyon");
    }

    private static void NesneModelOlustur(ModelBuilder builder)
    {
        builder.Entity<Tbl_Nesne>(entity =>
        {
            entity.ToTable("Tbl_Nesne", "dbo");

            entity.HasKey(x => x.Id)
                .HasName("PK_Tbl_Nesne");

            entity.Property(x => x.Id)
                .ValueGeneratedOnAdd();

            entity.Property(x => x.Makine_Id)
                .HasColumnName("Makine_Id")
                .IsRequired();
       
            entity.HasOne(x => x.Makine)
                .WithMany(x => x.Nesneler)
                .HasForeignKey(x => x.Makine_Id)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Tbl_Nesne_Tbl_Makine");
        });
    }

    private static void LogNesneModelOlustur(ModelBuilder builder)
    {
        builder.Entity<Tbl_LogNesne>(entity =>
        {
            entity.ToTable("Tbl_LogNesne", "dbo");

            entity.HasKey(x => x.Id)
                .HasName("PK_Tbl_LogNesne");

            entity.Property(x => x.Id)
                .ValueGeneratedOnAdd();

            entity.Property(x => x.Timestamp)
                .HasColumnName("Timestamp")
                .HasColumnType("datetime")
                .HasDefaultValueSql("(getdate())");

            entity.Property(x => x.Nesne_Id)
                .HasColumnName("Nesne_Id")
                .IsRequired();

            entity.Property(x => x.Value1_Type)
                .HasColumnName("Value1_Type");

            entity.Property(x => x.Value1)
                .HasColumnName("Value1")
                .HasPrecision(15, 3);

            entity.Property(x => x.Value2_Type)
                .HasColumnName("Value2_Type");

            entity.Property(x => x.Value2)
                .HasColumnName("Value2")
                .HasPrecision(15, 3);

            entity.Property(x => x.Value3_Type)
                .HasColumnName("Value3_Type");

            entity.Property(x => x.Value3)
                .HasColumnName("Value3")
                .HasPrecision(15, 3);

            entity.Property(x => x.NodeRedTimestamp)
                .HasColumnName("NodeRed_Timestamp")
                .HasColumnType("datetime");

            entity.HasOne(x => x.Nesne)
                .WithMany(x => x.Loglar)
                .HasForeignKey(x => x.Nesne_Id)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName(
                    "FK_Tbl_LogNesne_Tbl_Nesne");

            /*
             * Burada parametresiz WithMany() kullanma.
             * Her ilişkinin karşı koleksiyonu açıkça gösteriliyor.
             */
            entity.HasOne(x => x.Value1Type)
                .WithMany(x => x.Value1Loglari)
                .HasForeignKey(x => x.Value1_Type)
                .HasPrincipalKey(x => x.EnumDegeri)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName(
                    "FK_Tbl_LogNesne_Value1Type_Tbl_ValueType_Enum");

            entity.HasOne(x => x.Value2Type)
                .WithMany(x => x.Value2Loglari)
                .HasForeignKey(x => x.Value2_Type)
                .HasPrincipalKey(x => x.EnumDegeri)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName(
                    "FK_Tbl_LogNesne_Value2Type_Tbl_ValueType_Enum");

            entity.HasOne(x => x.Value3Type)
                .WithMany(x => x.Value3Loglari)
                .HasForeignKey(x => x.Value3_Type)
                .HasPrincipalKey(x => x.EnumDegeri)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName(
                    "FK_Tbl_LogNesne_Value3Type_Tbl_ValueType_Enum");
        });
    }
}