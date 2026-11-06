using HegCoolingScada.Application.Interfaces;
using HegCoolingScada.Application.Services;
using HegCoolingScada.Domain.Interfaces;
using HegCoolingScada.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace HegCoolingScada.Infrastructure
{
    public static class HegSogutmaScadaIoC
    {
        public static void RegisterServices(this IServiceCollection services)
        {
            services.RegisterAppServices();
            services.RegisterRepositories();
        }

        private static void RegisterAppServices(this IServiceCollection services)
        {
            services.AddScoped<IHegSogutmaRaporAppService, HegSogutmaRaporAppService>();
            services.AddScoped<IHegAlarmAppService, HegAlarmAppService>();
        }

        private static void RegisterRepositories(this IServiceCollection services)
        {
            services.AddScoped<IHegSogutmaRepository, HegSogutmaRepository>();
            services.AddScoped<IHegAlarmRepository, HegAlarmRepository>();
        }
    }
}
