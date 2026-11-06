using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MobilGuncelleme.Application.Interfaces;
using MobilGuncelleme.Application.Services;
using MobilGuncelleme.Infrastructure.Depolama;

namespace MobilGuncelleme.Infrastructure
{
    /// <summary>MobilGuncelleme modülünün tüm DI kayıtları.</summary>
    public static class DependencyInjection
    {
        /// <summary>
        /// Bağlantı dizesi anahtarı bilerek modüle özeldir. Sunucuda makine seviyesinde tanımlı
        /// ConnectionStrings__* değişkenleri appsettings'i ezer; "SqlServer" veya "Postgres" gibi
        /// ortak bir ad uygulamayı başka bir veritabanına bağlar (SiloScada'da yaşanan sorun).
        /// </summary>
        public const string ConnectionStringAdi = "MobilGuncellemePostgres";

        public static IServiceCollection RegisterServices(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString(ConnectionStringAdi);
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException($"ConnectionStrings:{ConnectionStringAdi} tanımlı değil.");

            services.AddDbContext<MobilGuncellemeDbContext>(options => options.UseNpgsql(connectionString));
            services.AddScoped<IMobilGuncellemeDbContext>(sp => sp.GetRequiredService<MobilGuncellemeDbContext>());

            services.Configure<MobilGuncellemeAyar>(configuration.GetSection(MobilGuncellemeAyar.Bolum));
            services.AddSingleton<IApkDepo, DiskApkDepo>();

            services.AddScoped<IMobilSurumAppService, MobilSurumAppService>();

            return services;
        }
    }
}
