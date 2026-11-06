using EnerjiIzleme.Application.Interfaces;
using EnerjiIzleme.Application.Services;
using EnerjiIzleme.Application.Services.Core;
using EnerjiIzleme.Domain.Interfaces;
using EnerjiIzleme.Infrastructure.Mongo;
using EnerjiIzleme.Infrastructure.Repositories;
using EnerjiIzleme.Infrastructure.Repositories.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Core;
using SharedKernel.Extensions;

namespace EnerjiIzleme.Infrastructure
{
    public static class EnerjiIzlemeIoc
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
            services.AddSingleton<IHayvanSayisiCacheMemory, HayvanSayisiCacheMemory>();
        }
        private static void RegisterAppServices(this IServiceCollection services)
        {
            services.AddScoped<IAlarmLogAppService, AlarmLogAppService>();
            services.AddScoped<IBolumAppService, BolumAppService>();
            services.AddScoped<ICihazAppService, CihazAppService>();
            services.AddScoped<IMakineAppService, MakineAppService>();
            services.AddScoped<ICihazModelAppService, CihazModelAppService>();
            services.AddScoped<IGatewayAppService, GatewayAppService>();
            services.AddScoped<ITreeAppService, TreeAppService>();

            services.AddSingleton<EnerjiIzlemeMongoDbContext>();
        }

        private static void RegisterDatabaseContexts(this IServiceCollection services)
        {
            //services.AddScoped<IUnitOfWork, UnitOfWork>();
        }

        private static void RegisterRepositories(this IServiceCollection services)
        {
            // MongoDB Repository ekle
            //services.AddScoped(typeof(IRepository<>), typeof(RepositoryForMongo<>));
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>)); // SQL için
            services.AddSingleton<MongoDbService>();
            services.AddSingleton<EnerjiIzlemeMongoDbContext>();

            services.AddScoped<IAlarmLogRepository, AlarmLogRepository>();
            services.AddScoped<IBolumRepository, BolumRepository>();
            services.AddScoped<ICihazRepository, CihazRepository>();
            services.AddScoped<ILogsRepository, LogsRepository>();
            services.AddScoped<IMakineRepository, MakineRepository>();
            services.AddScoped<ICihazModelRepository, CihazModelRepository>();
            services.AddScoped<IGatewayRepository, GatewayRepository>();
            services.AddScoped<ITreeRepository, TreeRepository>();
        }
    }
}