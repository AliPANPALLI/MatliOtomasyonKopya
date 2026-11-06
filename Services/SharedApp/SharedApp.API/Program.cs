using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using SharedApp.Application.Mapster;
using SharedApp.Infrastructure;
using MatliOtomasyon.Shared.Infrastructure.Configuration;
using MatliOtomasyon.Shared.Web.Errors;
using MatliOtomasyon.Shared.Web.Http;
using MatliOtomasyon.Shared.Web.Security;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentPlaceholders();
builder.Services.AddDbContext<SharedDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("SharedSqlServer"), sqlOptions =>
    {
        sqlOptions.MigrationsAssembly("SharedApp.Infrastructure");
    });
});

// Add services to the container.
builder.Services.Configure<CustomTokenOption>(builder.Configuration.GetSection("TokenOption"));
builder.Services.Configure<InternalServiceOptions>(builder.Configuration.GetSection(InternalServiceOptions.SectionName));
builder.Services.AddHttpContextAccessor();

//authentication
builder.Services.AddCustomTokenAuth(builder.Configuration.GetSection("TokenOption").Get<CustomTokenOption>());

builder.Services.AddAuthorization();

#region DI
//DI Register
builder.Services.RegisterServices();
#region Mapster
var mapsterConfig = new TypeAdapterConfig();
mapsterConfig.Scan(typeof(SharedAppMappingConfig).Assembly);
mapsterConfig.Compile();
builder.Services.AddSingleton(mapsterConfig);
builder.Services.AddScoped<IMapper, ServiceMapper>();
#endregion Mapster
builder.Services.AddResponseEncryption(builder.Configuration);
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "SharedApp.API", Version = "v1" })
);
#endregion DI

#region
builder.Services.AddCors(options =>
{
    options.AddPolicy(
        name: "AllowOrigin",
        builder =>
        {
            builder.WithOrigins("https://localhost:44313", "http://localhost:4200", "https://otomasyon.matli.com.tr", "https://otomasyontest.matli.com.tr")
                    .AllowAnyMethod()
                    .AllowAnyHeader();
        });
});
#endregion cors

var app = builder.Build();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
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

//app.UseSession();

app.Run();