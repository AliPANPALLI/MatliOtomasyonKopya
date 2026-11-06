using Microsoft.Extensions.DependencyInjection;
using SiloTemperatureMonitoring.Application.Interfaces;
using SiloTemperatureMonitoring.Application.Services;
using SiloTemperatureMonitoring.Domain.Interfaces;
using SiloTemperatureMonitoring.Infrastructure.Repositories;

namespace SiloTemperatureMonitoring.Infrastructure
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
