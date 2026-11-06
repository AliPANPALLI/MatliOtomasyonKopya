using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using WaterLineScada.Application.Interfaces;
using WaterLineScada.Application.Services;
using WaterLineScada.Application.Services.Core;
using WaterLineScada.Domain.Interfaces;
using WaterLineScada.Infrastructure.Repositories;

namespace WaterLineScada.Infrastructure
{
    public static class SuHatScadaIoC
    {
        public static void RegisterServices(this IServiceCollection services)
        {
            services.RegisterMiscellaneousServices();
            services.RegisterCachingServices();

            services.RegisterAppServices();
            services.RegisterDatabaseContexts();
            services.RegisterRepositories();
        }
        private static void RegisterMiscellaneousServices(this IServiceCollection services)
        {
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSingleton<HttpClient>();
            services.AddHttpClient();
        }
        private static void RegisterCachingServices(this IServiceCollection services)
        {
            services.AddScoped<DMGunlukCacheService>();
            services.AddScoped<SekilKuralCacheService>();
            services.AddMemoryCache();
        }
        private static void RegisterAppServices(this IServiceCollection services)
        {
            services.AddScoped<ICizimAppService, CizimAppService>();
            services.AddScoped<ISuKaynakAppService, SuKaynakAppService>();
        }

        private static void RegisterDatabaseContexts(this IServiceCollection services)
        {
            //services.AddScoped<IUnitOfWork, UnitOfWork>();
        }

        private static void RegisterRepositories(this IServiceCollection services)
        {
            services.AddScoped<ICizimRepository, CizimRepository>();
            services.AddScoped<IEtiketLogRepository, EtiketLogRepository>();
            services.AddScoped<IPlcAyarlarRepository, PlcAyarlarRepository>();
            services.AddScoped<ISayacRepository, SayacRepository>();
            services.AddScoped<ISuKaynakRepository, SuKaynakRepository>();
        }
    }
}