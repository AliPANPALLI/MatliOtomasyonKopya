using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjeTakip.Application.Interfaces;
using ProjeTakip.Application.Services;
using ProjeTakip.Domain.Interfaces;
using ProjeTakip.Infrastructure.Persistence;
using ProjeTakip.Infrastructure.Repositories;

namespace ProjeTakip.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddProjeTakipInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ProjeTakipSqlServer");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("ConnectionStrings:ProjeTakipSqlServer tanımlanmalıdır.");

        services.AddDbContext<ProjeTakipDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IProjeTakipRepository, ProjeTakipRepository>();
        services.AddScoped<IProjeTakipService, ProjeTakipService>();
        return services;
    }
}
