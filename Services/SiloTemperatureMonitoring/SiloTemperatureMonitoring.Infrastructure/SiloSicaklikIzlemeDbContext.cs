using Microsoft.EntityFrameworkCore;
using SiloTemperatureMonitoring.Domain.DbViewModels;
using SiloTemperatureMonitoring.Domain.Models;

namespace SiloTemperatureMonitoring.Infrastructure
{
    // Bu context, ITG tesisiyle paylaşılan ortak TimescaleDB şemasını (FACILITY/DEPOT/
    // SENSOR_CABLE/TEMPERATURE_SENSOR/TEMPERATURE_READING/...) salt okunur ağırlıklı
    // olarak eşler (bkz. HegSogutmaScadaDbContext). Bilerek migration içermiyor - şema
    // Servisler/ItgTemperatureIngestionWorker tarafındaki DDL ile zaten var; buradan asla
    // migrate edilmemeli. İkinci bir (IConfiguration alan) constructor da bilerek yok -
    // iki constructor DI'da "ambiguous" hatasına yol açıyordu (bkz. aynı not Heg'de).
    public class SiloSicaklikIzlemeDbContext : DbContext
    {
        public SiloSicaklikIzlemeDbContext(DbContextOptions<SiloSicaklikIzlemeDbContext> options) : base(options)
        {
        }

        public DbSet<Facility> Facilities => Set<Facility>();
        public DbSet<Depot> Depots => Set<Depot>();
        public DbSet<EdgeDevice> EdgeDevices => Set<EdgeDevice>();
        public DbSet<Controller> Controllers => Set<Controller>();
        public DbSet<SensorCable> SensorCables => Set<SensorCable>();
        public DbSet<TemperatureSensor> TemperatureSensors => Set<TemperatureSensor>();
        public DbSet<SensorLatestValue> SensorLatestValues => Set<SensorLatestValue>();
        public DbSet<TemperatureReading> TemperatureReadings => Set<TemperatureReading>();
        public DbSet<AlarmRule> AlarmRules => Set<AlarmRule>();
        public DbSet<AlarmEvent> AlarmEvents => Set<AlarmEvent>();

        public DbSet<TemperatureHourly> TemperatureHourly => Set<TemperatureHourly>();
        public DbSet<TemperatureDaily> TemperatureDaily => Set<TemperatureDaily>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Facility>(entity =>
            {
                entity.ToTable("FACILITY");
                entity.HasKey(x => x.Id);
            });

            builder.Entity<Depot>(entity =>
            {
                entity.ToTable("DEPOT");
                entity.HasKey(x => x.Id);
            });

            builder.Entity<EdgeDevice>(entity =>
            {
                entity.ToTable("EDGE_DEVICE");
                entity.HasKey(x => x.Id);
            });

            builder.Entity<Controller>(entity =>
            {
                // Tablo adı "ITG_CONTROLLER" tarihsel; şema facility-agnostic.
                entity.ToTable("ITG_CONTROLLER");
                entity.HasKey(x => x.Id);
            });

            builder.Entity<SensorCable>(entity =>
            {
                entity.ToTable("SENSOR_CABLE");
                entity.HasKey(x => x.Id);
            });

            builder.Entity<TemperatureSensor>(entity =>
            {
                entity.ToTable("TEMPERATURE_SENSOR");
                entity.HasKey(x => x.Id);
            });

            builder.Entity<SensorLatestValue>(entity =>
            {
                entity.ToTable("SENSOR_LATEST_VALUE");
                entity.HasKey(x => x.SensorId);
            });

            builder.Entity<TemperatureReading>(entity =>
            {
                entity.ToTable("TEMPERATURE_READING");
                entity.HasKey(x => new { x.SensorId, x.MeasuredAt });
            });

            builder.Entity<AlarmRule>(entity =>
            {
                entity.ToTable("ALARM_RULE");
                entity.HasKey(x => x.Id);
            });

            builder.Entity<AlarmEvent>(entity =>
            {
                entity.ToTable("ALARM_EVENT");
                entity.HasKey(x => x.Id);
            });

            ConfigureBucketView<TemperatureHourly>(builder, "TEMPERATURE_HOURLY");
            ConfigureBucketView<TemperatureDaily>(builder, "TEMPERATURE_DAILY");
        }

        private static void ConfigureBucketView<TEntity>(ModelBuilder builder, string viewName)
            where TEntity : TemperatureBucketBase
        {
            builder.Entity<TEntity>(entity =>
            {
                entity.HasNoKey();
                entity.ToView(viewName);
            });
        }
    }
}
