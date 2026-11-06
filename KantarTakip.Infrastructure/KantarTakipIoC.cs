
using KantarTakip.Application.Interfaces;
using KantarTakip.Application.Services;
using KantarTakip.Domain.Interfaces;
using KantarTakip.Infrastructure.Integrations.SAP;
using KantarTakip.Infrastructure.Repositories;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Extensions;
using System;
using System.Net.Http;

namespace KantarTakip.Infrastructure
{
    public static class KantarTakipIoC
    {
        // DbContext Program.cs içerisinde kayıtlı.
        public static void RegisterServices(
            this IServiceCollection services)
        {
            services.RegisterMiscellaneousServices();
            services.RegisterCachingServices();
            services.RegisterAppServices();
            services.RegisterRepositories();
            services.RegisterSapServices();
        }

        #region Miscellaneous

        private static void RegisterMiscellaneousServices(
            this IServiceCollection services)
        {
            services.AddHttpContextAccessor();

            services.AddScoped<IUserExtension, UserExtension>();
        }

        #endregion

        #region Caching

        private static void RegisterCachingServices(
            this IServiceCollection services)
        {
            services.AddMemoryCache();
        }

        #endregion

        #region Application Services

        private static void RegisterAppServices(
            this IServiceCollection services)
        {
            services.AddScoped<
                IKantarTakipAppService,
                KantarTakipAppService>();

            // Kantar KPI ve süreç raporlama servisi
            services.AddScoped<
                IKantarKpiService,
                KantarKpiService>();
        }

        #endregion

        #region SAP Integration

        private static void RegisterSapServices(
            this IServiceCollection services)
        {
            services.AddHttpClient();

            services.AddScoped<ISapKantarService>(provider =>
            {
                var httpClientFactory =
                    provider.GetRequiredService<IHttpClientFactory>();

                var httpClient =
                    httpClientFactory.CreateClient();

                httpClient.Timeout = TimeSpan.FromSeconds(60);

                var configuration =
                    provider.GetRequiredService<IConfiguration>();

                var cache =
                    provider.GetRequiredService<IMemoryCache>();

                return new SapKantarService(
                    httpClient,
                    configuration,
                    cache);
            });
        }

        #endregion

        #region Repositories

        private static void RegisterRepositories(
            this IServiceCollection services)
        {
            services.AddScoped<
                IKantarTakipRepository,
                KantarTakipRepository>();
        }

        #endregion
    }
}
