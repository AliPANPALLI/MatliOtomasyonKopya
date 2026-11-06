using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using ObjectMonitoring.Application.Interfaces;
using ObjectMonitoring.Application.Services;
using ObjectMonitoring.Domain.Interfaces;
using ObjectMonitoring.Infrastructure.Repositories;
using SharedKernel.Extensions;

namespace ObjectMonitoring.Infrastructure
{
    public static class NesneIzlemeIoC
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
            //services.AddMemoryCache();
        }
        private static void RegisterAppServices(this IServiceCollection services)
        {
            services.AddScoped<IDebimetreAppService, DebimetreAppService>();
        }

        private static void RegisterDatabaseContexts(this IServiceCollection services)
        {
            //services.AddScoped<IUnitOfWork, UnitOfWork>();
        }

        private static void RegisterRepositories(this IServiceCollection services)
        {
            services.AddScoped<IDebimetreRepository, DebimetreRepository>();
        }
    }
}
