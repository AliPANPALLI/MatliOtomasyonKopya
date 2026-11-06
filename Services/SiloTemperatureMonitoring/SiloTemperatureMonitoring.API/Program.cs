using Microsoft.EntityFrameworkCore;
using SiloTemperatureMonitoring.Infrastructure;
using MatliOtomasyon.Shared.Infrastructure.Configuration;
using MatliOtomasyon.Shared.Web.Errors;
using MatliOtomasyon.Shared.Web.Security;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentPlaceholders();

builder.Services.AddDbContext<SiloSicaklikIzlemeDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres"));
});

// Add services to the container.
builder.Services.Configure<CustomTokenOption>(builder.Configuration.GetSection("TokenOption"));
builder.Services.AddHttpContextAccessor();

//authentication
builder.Services.AddCustomTokenAuth(builder.Configuration.GetSection("TokenOption").Get<CustomTokenOption>());

builder.Services.AddAuthorization();

#region DI
builder.Services.RegisterServices();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "SiloTemperatureMonitoring.API", Version = "v1" })
);
#endregion DI

#region cors
builder.Services.AddCors(options =>
{
    options.AddPolicy(
        name: "AllowSpecificOrigin",
        policy =>
        {
            policy.WithOrigins("https://localhost:44313", "http://localhost:4200", "https://otomasyon.matli.com.tr", "https://otomasyontest.matli.com.tr")
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        });
});
#endregion cors

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowSpecificOrigin");

app.UseExceptionHandler(appError => appError.UseMiddleware<ExceptionMiddleware>());

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
