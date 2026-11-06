using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using PanoKimliklendirme.Application.Interfaces;
using PanoKimliklendirme.Application.Services;
using PanoKimliklendirme.Domain.Interfaces;
using PanoKimliklendirme.Infrastructure.Repositories;
using SharedKernel.Extensions;

namespace PanoKimliklendirme.Infrastructure
{
    public static class PanoKimliklendirmeIoc
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
            services.AddScoped<IUserExtension, UserExtension>();
            services.AddSingleton<HttpClient>();
        }
        private static void RegisterCachingServices(this IServiceCollection services)
        {
            services.AddMemoryCache();
        }
        private static void RegisterAppServices(this IServiceCollection services)
        {
            services.AddScoped<IBolumAppService, BolumAppService>();
            services.AddScoped<ICihazAppService, CihazAppService>();
            services.AddScoped<IMakineAppService, MakineAppService>();
            services.AddScoped<IOnayIslemKayitAppService, OnayIslemKayitAppService>();
            services.AddScoped<IPanoAppService, PanoAppService>();
            services.AddScoped<ITreeAppService, TreeAppService>();
        }

        private static void RegisterDatabaseContexts(this IServiceCollection services)
        {
            //services.AddScoped<IUnitOfWork, UnitOfWork>();
        }

        private static void RegisterRepositories(this IServiceCollection services)
        {
            services.AddScoped<IBolumRepository, BolumRepository>();
            services.AddScoped<ICihazRepository, CihazRepository>();
            services.AddScoped<ILokasyonDosyaYoluEslemeRepository, LokasyonDosyaYoluEslemeRepository>();
            services.AddScoped<IMakineRepository, MakineRepository>();
            services.AddScoped<IOnayIslemKayitRepository, OnayIslemKayitRepository>();
            services.AddScoped<IPanoRepository, PanoRepository>();
            services.AddScoped<IPanoMakineEslemeRepository, PanoMakineEslemeRepository>();
            services.AddScoped<ITreeRepository, TreeRepository>();
        }
    }
}
