using PoultryHouseScada.API.SignalR.KumesScada;
using PoultryHouseScada.API.SignalR.Test;
using PoultryHouseScada.Application.Mapster;
using PoultryHouseScada.Application.Services;
using PoultryHouseScada.Infrastructure;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using MatliOtomasyon.Shared.Infrastructure.Configuration;
using MatliOtomasyon.Shared.Web.Errors;
using MatliOtomasyon.Shared.Web.Security;

var builder = WebApplication.CreateBuilder(args);
var dbUser = Environment.GetEnvironmentVariable("DB_USER");
var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");

var connectionString = builder.Configuration.GetConnectionString("SqlServer")
    .Replace("{DB_USER}", dbUser)
    .Replace("{DB_PASSWORD}", dbPassword);
builder.Services.AddDbContext<KumesScadaDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer"), sqlOptions =>
    {
        sqlOptions.MigrationsAssembly("PoultryHouseScada.Infrastructure");
    });
});

// Add services to the container.
builder.Services.Configure<CustomTokenOption>(builder.Configuration.GetSection("TokenOption"));
builder.Services.AddHttpContextAccessor();

//authentication
builder.Services.AddCustomTokenAuth(builder.Configuration.GetSection("TokenOption").Get<CustomTokenOption>());

builder.Services.AddSignalR(options =>
{
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
});
builder.Services.AddHostedService(provider => provider.GetRequiredService<VeriGuncellemeBackgroundService>());
builder.Services.AddHostedService(provider => provider.GetRequiredService<TestBackgroundService>());
builder.Services.AddSingleton<VeriGuncellemeBackgroundService>();
builder.Services.AddSingleton<TestBackgroundService>();
//builder.Services.AddHostedService<KumesAlarmBackgroundService>();
//builder.Services.AddHostedService<KumesRaporBackgroundService>();

builder.Services.AddAuthorization();

#region DI
//DI Register
builder.Services.RegisterServices();
builder.Services.AddSingleton<RedisEtiketService>(sp => new RedisEtiketService(builder.Configuration.GetConnectionString("Redis")!));
//builder.Services.AddAutoMapper(cfg =>
//{
//    cfg.AddProfile<KumesScadaMappingProfile>();
//});
#region Mapster
var mapsterConfig = new TypeAdapterConfig();
mapsterConfig.Scan(typeof(KumesScadaMappingConfig).Assembly);
mapsterConfig.Compile();
builder.Services.AddSingleton(mapsterConfig);
builder.Services.AddScoped<MapsterMapper.IMapper, ServiceMapper>();
#endregion Mapster
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "PoultryHouseScada.API", Version = "v1" })
);
#endregion DI

#region
builder.Services.AddCors(options =>
{
    options.AddPolicy(
    name: "AllowSpecificOrigin",
        builder =>
        {
            builder.WithOrigins("https://localhost:44313", "http://localhost:4200", "https://otomasyon.matli.com.tr", "https://otomasyontest.matli.com.tr")
                   .AllowAnyMethod()
                   .AllowAnyHeader()
                   .AllowCredentials();
        });
});
#endregion cors

var app = builder.Build();
using (var serviceScope = app.Services.GetRequiredService<IServiceScopeFactory>().CreateScope())
{
    var context = serviceScope.ServiceProvider.GetRequiredService<KumesScadaDbContext>();
    //var result = context.Database.EnsureCreated();
}

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowSpecificOrigin");
//middleware
app.UseExceptionHandler(appError => appError.UseMiddleware<ExceptionMiddleware>());

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

//app.UseSession();
app.MapHub<SignalRHub>("/signalrHub");
//app.MapHub<KumesRaporHub>("/kumesRaporHub");
//app.MapHub<PoultryHouseScada.API.KumesAlarmHub>("/kumesAlarmHub");
app.MapHub<TestHub>("/testHub");

app.Run();
