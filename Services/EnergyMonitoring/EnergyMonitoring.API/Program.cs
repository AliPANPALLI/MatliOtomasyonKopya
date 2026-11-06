using EnergyMonitoring.API.SignalR;
using EnergyMonitoring.Application.Mapster;
using EnergyMonitoring.Infrastructure;
using EnergyMonitoring.Infrastructure.Mongo;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using SharedKernel.Middlewares;

ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<EnerjiIzlemeDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer"), sqlOptions =>
    {
        sqlOptions.MigrationsAssembly("EnergyMonitoring.Infrastructure");
    });
});

builder.Services.AddSingleton<MongoDbService>();

var settings = builder.Configuration.GetSection("MongoDbSettings").Get<MongoDbSettings>();
//// MongoDB Repository ekle
builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDbSettings"));

builder.Services.RegisterServices();

builder.Services.AddSignalR();
builder.Services.AddHostedService(provider => provider.GetRequiredService<KumesEnerjiIzlemeBackgroundService>());
builder.Services.AddSingleton<KumesEnerjiIzlemeBackgroundService>();
builder.Services.AddSingleton<IKumesEnerjiSubscriptionStore, KumesEnerjiSubscriptionStore>();

#region Mapster
var mapsterConfig = new TypeAdapterConfig();
mapsterConfig.Scan(typeof(EnerjiIzlemeMappingConfig).Assembly);
mapsterConfig.Compile();
builder.Services.AddSingleton(mapsterConfig);
builder.Services.AddScoped<MapsterMapper.IMapper, ServiceMapper>();
#endregion Mapster
builder.Services.AddControllers();
builder.Services.AddSwaggerGen(c =>
c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "EnergyMonitoring.API", Version = "v1" })
);

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        name: "AllowOrigin",
        builder =>
        {
            builder.AllowAnyOrigin()//domain yaziliyor
                    .AllowAnyMethod()
                    .AllowAnyHeader();
        });
});

var app = builder.Build();

//if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowOrigin");
//middleware
app.UseExceptionHandler(appError => appError.UseMiddleware<ExceptionMiddleware>());

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.MapHub<KumesEnerjiIzlemeHub>("/kumesEnerjiIzlemeHub");

app.Run();