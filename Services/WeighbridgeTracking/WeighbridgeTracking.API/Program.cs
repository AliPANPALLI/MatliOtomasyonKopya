using WeighbridgeTracking.API.Filters;
using WeighbridgeTracking.Application.Mapster;
using WeighbridgeTracking.Infrastructure;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using MatliOtomasyon.Shared.Infrastructure.Configuration;
using MatliOtomasyon.Shared.Web.Errors;
using MatliOtomasyon.Shared.Web.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<KantarTakipDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer"), sqlOptions =>
    {
        sqlOptions.MigrationsAssembly("WeighbridgeTracking.Infrastructure");
    });
});

// Add services to the container.
builder.Services.Configure<CustomTokenOption>(builder.Configuration.GetSection("TokenOption"));

//authentication
builder.Services.AddCustomTokenAuth(builder.Configuration.GetSection("TokenOption").Get<CustomTokenOption>());

builder.Services.AddAuthorization();

#region DI
//DI Register
builder.Services.RegisterServices();
#region Mapster
var mapsterConfig = new TypeAdapterConfig();
mapsterConfig.Scan(typeof(KantarTakipMappingConfig).Assembly);
mapsterConfig.Compile();
builder.Services.AddSingleton(mapsterConfig);
builder.Services.AddScoped<IMapper, ServiceMapper>();
#endregion Mapster
builder.Services.AddControllers(options =>
    options.Filters.Add<KantarTakipExceptionFilter>());
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "WeighbridgeTracking.API", Version = "v1" });

    // Controller'lar [Authorize] olduğu için Swagger'dan token ile test edilebilsin.
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});
#endregion DI

#region cors
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

//// NOT: Migration'ı burada otomatik uygulamak istiyorsanız (dotnet ef migrations add sonrası):
//using (var serviceScope = app.Services.GetRequiredService<IServiceScopeFactory>().CreateScope())
//{
//    var context = serviceScope.ServiceProvider.GetRequiredService<KantarTakipDbContext>();
//    context.Database.Migrate();
//}

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
app.Run();
