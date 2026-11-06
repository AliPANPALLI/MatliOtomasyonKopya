using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using EnvironmentMonitoringScada.Application.Mapster;
using EnvironmentMonitoringScada.Infrastructure;
using MatliOtomasyon.Shared.Web.Errors;


var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<OrtamIzlemeScadaDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer"), sqlOptions =>
    {
        sqlOptions.MigrationsAssembly("EnvironmentMonitoringScada.Infrastructure");
    });
});


builder.Services.RegisterServices();
#region Mapster
var mapsterConfig = new TypeAdapterConfig();
mapsterConfig.Scan(typeof(OrtamIzlemeScadaMappingConfig).Assembly);
mapsterConfig.Compile();
builder.Services.AddSingleton(mapsterConfig);
builder.Services.AddScoped<MapsterMapper.IMapper, ServiceMapper>();
#endregion Mapster
builder.Services.AddControllers();
builder.Services.AddSwaggerGen(c =>
c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "EnvironmentMonitoringScada.API", Version = "v1" })
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

app.Run();