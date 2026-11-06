using MobileUpdate.API.Middlewares;
using MobileUpdate.Infrastructure;
using MatliOtomasyon.Shared.Infrastructure.Configuration;
using MatliOtomasyon.Shared.Web.Security;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentPlaceholders();

// Connection string "MobilGuncellemePostgres", APK klasörü "MobilGuncelleme:ApkKlasoru" ayarından okunur.
builder.Services.RegisterServices(builder.Configuration);

builder.Services.Configure<CustomTokenOption>(builder.Configuration.GetSection("TokenOption"));
builder.Services.AddHttpContextAccessor();

//authentication
builder.Services.AddCustomTokenAuth(builder.Configuration.GetSection("TokenOption").Get<CustomTokenOption>());
builder.Services.AddAuthorization();

// Admin endpoint'leri form yetkisini SharedApp'ten okur (FormYetkiAttribute).
builder.Services.AddHttpClient();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "MobileUpdate.API", Version = "v1" })
);

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

app.UseExceptionHandler(appError => appError.UseMiddleware<HataYonetimiMiddleware>());

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
