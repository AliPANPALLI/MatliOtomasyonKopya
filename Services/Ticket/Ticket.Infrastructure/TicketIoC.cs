using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Ticket.Application.Interfaces;
using Ticket.Application.Services;
using Ticket.Domain.Interfaces;
using Ticket.Infrastructure.Repositories;
using MatliOtomasyon.Shared.Application.Abstractions;
using MatliOtomasyon.Shared.Web.Security;

namespace Ticket.Infrastructure
{
    public static class TicketIoC
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
            services.AddScoped<ITicketTanimAppService, TicketTanimAppService>();
            services.AddScoped<ITedarikciAppService, TedarikciAppService>();
        }

        private static void RegisterDatabaseContexts(this IServiceCollection services)
        {
            //services.AddScoped<IUnitOfWork, UnitOfWork>();
        }

        private static void RegisterRepositories(this IServiceCollection services)
        {
            services.AddScoped<ITicketTanimRepository, TicketTanimRepository>();
            services.AddScoped<ITicketDurumRepository, TicketDurumRepository>();
            services.AddScoped<ITicketLogsRepository, TicketLogsRepository>();
            services.AddScoped<ITedarikciRepository, TedarikciRepository>();
            services.AddScoped<ITicketMatliTakipciEslemeRepository, TicketMatliTakipciEslemeRepository>();
            services.AddScoped<ITicketSorumluUserEslemeRepository, TicketSorumluUserEslemeRepository>();
        }
    }
}
