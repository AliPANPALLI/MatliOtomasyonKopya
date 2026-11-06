using Microsoft.Extensions.DependencyInjection;
using SiloSicaklikIzleme.Application.Interfaces;
using SiloSicaklikIzleme.Application.Services;
using SiloSicaklikIzleme.Domain.Interfaces;
using SiloSicaklikIzleme.Infrastructure.Repositories;

namespace SiloSicaklikIzleme.Infrastructure
{
    public static class SiloSicaklikIzlemeIoC
    {
        public static void RegisterServices(this IServiceCollection services)
        {
            services.RegisterAppServices();
            services.RegisterRepositories();
        }

        private static void RegisterAppServices(this IServiceCollection services)
        {
            services.AddScoped<ISiloSicaklikRaporAppService, SiloSicaklikRaporAppService>();
        }

        private static void RegisterRepositories(this IServiceCollection services)
        {
            services.AddScoped<ISiloSicaklikRepository, SiloSicaklikRepository>();
        }
    }
}
