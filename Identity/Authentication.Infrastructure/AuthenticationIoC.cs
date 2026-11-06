using Authentication.Application.Interfaces;
using Authentication.Application.Services;
using Authentication.Application.ViewModels;
using Authentication.Domain.Interfaces;
using Authentication.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SharedApp.Infrastructure.Sender;
using MatliOtomasyon.Shared.Application.Abstractions;
using MatliOtomasyon.Shared.Web.Security;

namespace Authentication.Infrastructure
{
    public static class AuthenticationIoC
    {
        public static void RegisterServices(this IServiceCollection services)
        {
            services.RegisterMiscellaneousServices();
            services.RegisterCachingServices();
            services.RegisterIdentityServices();

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
        private static void RegisterIdentityServices(this IServiceCollection services)
        {
            services.AddScoped<IAuthenticationService, AuthenticationService>();
            services.AddSingleton<PasswordHash>();
            services.AddScoped<EmailSender>();
            services.AddScoped<SmsSender>();
        }
        private static void RegisterAppServices(this IServiceCollection services)
        {
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<ITwoFactorService, TwoFactorService>();
        }

        private static void RegisterDatabaseContexts(this IServiceCollection services)
        {
            //services.AddScoped<IUnitOfWork, UnitOfWork>();
        }

        private static void RegisterRepositories(this IServiceCollection services)
        {
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<ITokenRepository, TokenRepository>();
            services.AddScoped<ITwoFactorRepository, TwoFactorRepository>();
        }
    }
}
