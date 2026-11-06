using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using MachineMonitoring.Application.Services;
using MachineMonitoring.Application.Interfaces;
using MachineMonitoring.Domain.Interfaces;
using MachineMonitoring.Infrastructure.Repositories;
using MatliOtomasyon.Shared.Application.Abstractions;
using MatliOtomasyon.Shared.Web.Security;

namespace MachineMonitoring.Infrastructure
{
    public static class MakineIzlemeIoC
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
            services.AddScoped<IMakineIzlemeAppService, MakineIzlemeAppService>();
        }

        private static void RegisterDatabaseContexts(this IServiceCollection services)
        {
            //services.AddScoped<IUnitOfWork, UnitOfWork>();
        }

        private static void RegisterRepositories(this IServiceCollection services)
        {
            services.AddScoped<IMakineIzlemeRepository, MakineIzlemeRepository>();
        }
    }
}