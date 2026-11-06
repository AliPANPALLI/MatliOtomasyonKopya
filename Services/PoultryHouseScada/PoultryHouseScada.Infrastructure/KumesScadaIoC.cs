using PoultryHouseScada.Application.Interfaces;
using PoultryHouseScada.Application.Services;
using PoultryHouseScada.Domain.Interfaces;
using PoultryHouseScada.Infrastructure.Excel;
using PoultryHouseScada.Infrastructure.Repositories;
using PoultryHouseScada.Infrastructure.Services;
using PoultryHouseScada.Infrastructure.Services.Excel;
using PoultryHouseScada.Infrastructure.Services.VG;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Extensions;

namespace PoultryHouseScada.Infrastructure
{
    public static class KumesScadaIoc
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
            services.AddScoped<ICizimAppService, CizimAppService>();
            services.AddScoped<IKonveyorAppService, KonveyorAppService>();
            services.AddScoped<IKumesAppService, KumesAppService>();
            services.AddScoped<IKumesHaftalikCatiKontrolAppService, KumesHaftalikCatiKontrolAppService>();
            services.AddScoped<IKumesRaporRehberAppService, KumesRaporRehberAppService>();
            services.AddScoped<IRaporVeriAppService, RaporVeriAppService>();
            services.AddScoped<INekropsiRaporuAppService, NekropsiRaporuAppService>();
        }

        private static void RegisterDatabaseContexts(this IServiceCollection services)
        {
            //services.AddScoped<IUnitOfWork, UnitOfWork>();
        }

        private static void RegisterRepositories(this IServiceCollection services)
        {
            services.AddScoped<IAlarmLogRepository, AlarmLogRepository>();
            services.AddScoped<ICizimRepository, CizimRepository>();
            services.AddScoped<IDonemRepository, DonemRepository>();
            services.AddScoped<IEtiketLogTempRepository, EtiketLogTempRepository>();
            services.AddScoped<IKonveyorRepository, KonveyorRepository>();
            services.AddScoped<IKumesHaftalikCatiKontrolRepository, KumesHaftalikCatiKontrolRepository>();
            services.AddScoped<IKumesHayvanLogRepository, KumesHayvanLogRepository>();
            services.AddScoped<IKumesHayvanRepository, KumesHayvanRepository>();
            services.AddScoped<IKumesLogRepository, KumesLogRepository>();
            services.AddScoped<IKumesLogTempRepository, KumesLogTempRepository>();
            services.AddScoped<IKumesRepository, KumesRepository>();
            services.AddScoped<IKumesRaporRehberRepository, KumesRaporRehberRepository>();
            services.AddScoped<IRaporVeriRepository, RaporVeriRepository>();
            services.AddScoped<INekropsiRaporuRepository, NekropsiRaporuRepository>();
            services.AddScoped<
                IVGExcelImportService,
                VGExcelImportService>();

            services.AddScoped<
                IVGExcelReader,
                VGExcelReader>();
            services.AddScoped<
                IVGKumesTasnifExcelReader,
                VGKumesTasnifExcelReader>();
            services.AddScoped<
                IVGVeriGirisTablosuExcelReader,
                VGVeriGirisTablosuExcelReader>();
            services.AddScoped<
                IRaporVeriExcelReader,
                RaporVeriExcelReader>();
            services.AddScoped<
                IVgYumurtlamaDonemAppService,
                VgYumurtlamaDonemAppService>();
            services.AddScoped<
                IVGYumurtlamaKartExcelReader,
                VGYumurtlamaKartExcelReader>();
            services.AddScoped<
                IVGYumurtlamaExcelSourceService,
                VGYumurtlamaExcelSourceService>();
            services.AddScoped<
                IVGYumurtlamaService,
                VGYumurtlamaService>();

            services.AddScoped<
                IVGYumurtlamaQueryService,
                VGYumurtlamaQueryService>();
            services.AddScoped<
                IVGYumurtlamaGenelVeriService,
                VGYumurtlamaGenelVeriService>();
            services.AddScoped<
                IVGYumurtlamaSaveService,
                VGYumurtlamaSaveService>();
            services.AddScoped<
                IVGYumurtlamaCalculationService,
                VGYumurtlamaCalculationService>();
            services.AddScoped<
                IVGYumurtlamaReportService,
                VGYumurtlamaReportService>();
            services.AddScoped<
                IVGYumurtlamaExcelExportService,
                VGYumurtlamaExcelExportService>();
            services.AddScoped<
                IVGYumurtlamaExcelGenerator,
                VGYumurtlamaExcelGenerator>();
        }
    }
}
