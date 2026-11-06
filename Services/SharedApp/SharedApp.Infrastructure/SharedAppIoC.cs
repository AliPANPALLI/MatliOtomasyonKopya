using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SharedApp.Application.Interfaces;
using SharedApp.Application.Services;
using SharedApp.Application.Services.Core;
using SharedApp.Domain.Interfaces;
using SharedApp.Infrastructure.Repositories;
using SharedApp.Infrastructure.Sender;
using MatliOtomasyon.Shared.Application.Abstractions;
using MatliOtomasyon.Shared.Web.Security;

namespace SharedApp.Infrastructure
{
    public static class SharedAppIoC
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
            services.AddScoped<EmailSender>();
            services.AddScoped<SesliMesajSender>();
            services.AddSingleton<SmsSender>();
            //services.AddSingleton<EmailSender>();
        }
        private static void RegisterCachingServices(this IServiceCollection services)
        {
            services.AddScoped<UserCacheService>();
            services.AddMemoryCache();
        }
        private static void RegisterAppServices(this IServiceCollection services)
        {
            services.AddScoped<IAlarmGruplarAppService, AlarmGruplarAppService>();
            services.AddScoped<IAlarmKullaniciEslemeAppService, AlarmKullaniciEslemeAppService>();
            services.AddScoped<IAlarmlarAppService, AlarmlarAppService>();
            services.AddScoped<IAlarmLogAppService, AlarmLogAppService>();
            services.AddScoped<IFormLokasyonEslemeAppService, FormLokasyonEslemeAppService>();
            services.AddScoped<IFormAppService, FormAppService>();
            services.AddScoped<IFormLinkEslemeAppService, FormLinkEslemeAppService>();
            services.AddScoped<IGenelParametrelerAppService, GenelParametrelerAppService>();
            services.AddScoped<ILokasyonAppService, LokasyonAppService>();
            services.AddScoped<IMailGonderimTakipAppService, MailGonderimTakipAppService>();
            services.AddScoped<IModulAppService, ModulAppService>();
            services.AddScoped<IProductionReportAppService, ProductionReportAppService>();
            services.AddScoped<ISirketAppService, SirketAppService>();
            services.AddScoped<ISonGelistirmelerAppService, SonGelistirmelerAppService>();
            services.AddScoped<IUserFormYetkiAppService, UserFormYetkiAppService>();
            services.AddScoped<IUserEnerjiIzlemeYetkiAppService, UserEnerjiIzlemeYetkiAppService>();
            services.AddScoped<IUserKumesYetkiAppService, UserKumesYetkiAppService>();
            services.AddScoped<IUserLinkYetkiAppService, UserLinkYetkiAppService>();
            services.AddScoped<IUserMakineYetkiAppService, UserMakineYetkiAppService>();
            services.AddScoped<IUserSuKaynakYetkiAppService, UserSuKaynakYetkiAppService>();
            services.AddScoped<IUserYetkiAppService, UserYetkiAppService>();
            services.AddScoped<IYanginFotografDosyaService, YanginFotografDosyaService>();
            services.AddScoped<IYetkiAppService, YetkiAppService>();
        }

        private static void RegisterDatabaseContexts(this IServiceCollection services)
        {
            //services.AddScoped<IUnitOfWork, UnitOfWork>();
        }

        private static void RegisterRepositories(this IServiceCollection services)
        {
            services.AddScoped<IAlarmGrupAlarmEslemeRepository, AlarmGrupAlarmEslemeRepository>();
            services.AddScoped<IAlarmGruplarRepository, AlarmGruplarRepository>();
            services.AddScoped<IAlarmKullaniciEslemeRepository, AlarmKullaniciEslemeRepository>();
            services.AddScoped<IAlarmlarRepository, AlarmlarRepository>();
            services.AddScoped<IAlarmLogRepository, AlarmLogRepository>();
            services.AddScoped<IFormLokasyonEslemeRepository, FormLokasyonEslemeRepository>();
            services.AddScoped<IFormRepository, FormRepository>();
            services.AddScoped<IFormLinkEslemeRepository, FormLinkEslemeRepository>();
            services.AddScoped<IGenelParametrelerRepository, GenelParametrelerRepository>();
            services.AddScoped<ILokasyonRepository, LokasyonRepository>();
            services.AddScoped<IMailGonderimTakipRepository, MailGonderimTakipRepository>();
            services.AddScoped<IModulRepository, ModulRepository>();
            services.AddScoped<IProductionReportRepository, ProductionReportRepository>();
            services.AddScoped<ISirketRepository, SirketRepository>();
            services.AddScoped<ISonGelistirmelerRepository, SonGelistirmelerRepository>();
            services.AddScoped<IUserFormYetkiRepository, UserFormYetkiRepository>();
            services.AddScoped<IUserEnerjiIzlemeYetkiRepository, UserEnerjiIzlemeYetkiRepository>();
            services.AddScoped<IUserKumesYetkiRepository, UserKumesYetkiRepository>();
            services.AddScoped<IUserLinkYetkiRepository, UserLinkYetkiRepository>();
            services.AddScoped<IUserMakineYetkiRepository, UserMakineYetkiRepository>();
            services.AddScoped<IUserSuKaynakYetkiRepository, UserSuKaynakYetkiRepository>();
            services.AddScoped<IUserYetkiRepository, UserYetkiRepository>();
            services.AddScoped<IYetkiRepository, YetkiRepository>();
        }
    }
}