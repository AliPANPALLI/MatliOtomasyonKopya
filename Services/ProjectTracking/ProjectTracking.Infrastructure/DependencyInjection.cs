using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjectTracking.Application.Interfaces;
using ProjectTracking.Application.Services;
using ProjectTracking.Domain.Interfaces;
using ProjectTracking.Infrastructure.Persistence;
using ProjectTracking.Infrastructure.Repositories;

namespace ProjectTracking.Infrastructure;

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
