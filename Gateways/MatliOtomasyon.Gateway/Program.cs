using MatliOtomasyon.Gateway;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Serilog;
using SharedKernel;
using SharedKernel.Configurations;
using SharedKernel.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile($"configuration.{builder.Environment.EnvironmentName}.json");

//builder.Services.AddHttpClient<TokenExchangeDelegateHandler>();
builder.Services.Configure<CustomTokenOption>(builder.Configuration.GetSection("TokenOption"));
builder.Services.AddHttpContextAccessor();

//authentication
builder.Services.AddCustomTokenAuth(builder.Configuration.GetSection("TokenOption").Get<CustomTokenOption>());

builder.Services.AddAuthorization();

builder.Services.AddOcelot().AddDelegatingHandler<TokenExchangeDelegateHandler>();
builder.Services.AddSignalR();

#region cors
builder.Services.AddCors(options =>
{
    options.AddPolicy(
      name: "AllowSpecificOrigin",
      builder =>
      {
          builder.WithOrigins("http://localhost:4200", "https://otomasyon.matli.com.tr", "https://otomasyontest.matli.com.tr"/*, "https://otomasyon.matli.com.tr:44313"*/)
                 .AllowAnyMethod()
                 .AllowAnyHeader()
                 .AllowCredentials();
      });
});
#endregion cors
Log.Logger = new LoggerConfiguration()
 //.ReadFrom.Configuration(builder.Configuration) // appsettings.json'dan yap�land�rmay� okur
 .WriteTo.Seq(ApiUrls.SeqLogServerUrl) // Seq sunucunuzun adresi
    .Enrich.FromLogContext()
    .CreateLogger();
builder.Host.UseSerilog();
// Forwarded headers' aktiflestir
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Eger proxy arkas�ndaysan kendi IP'ni g�venilir proxy olarak ekle:
    options.KnownProxies.Add(System.Net.IPAddress.Parse("127.0.0.1"));
});
var app = builder.Build();
app.UseForwardedHeaders();
//app.Use(async (context, next) =>
//{
//    // Never forward client-supplied IP headers as an authoritative source.
//    var unresolvedProxy = context.Request.Headers.ContainsKey("X-Forwarded-For");
//    context.Request.Headers["X-Forwarded-For"] = unresolvedProxy ? "unknown" : context.Connection.RemoteIpAddress?.ToString();
//    context.Request.Headers.Remove("X-Real-IP");
//    await next();
//});

app.UseWebSockets(); // SignalR i�in gerekli
app.UseRouting();

app.UseHttpsRedirection();

app.Lifetime.ApplicationStopped.Register(() => Log.CloseAndFlush());
app.UseCors("AllowSpecificOrigin");
// Authentication ve Ocelot middleware'lerini ekleme
app.UseAuthentication();
app.UseAuthorization();
app.Use(async (context, next) =>
{
    Console.WriteLine($"[OCELOT] Incoming URL: {context.Request.Method} {context.Request.Path}{context.Request.QueryString}");
    Console.WriteLine("Ocelot Incoming XFF: " + context.Request.Headers["X-Forwarded-For"].ToString());
 
    if (context.WebSockets.IsWebSocketRequest && context.Request.Path.StartsWithSegments("/ws"))
    {
        var result = await context.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);
        if (!result.Succeeded)
        {
            Console.WriteLine($"[WS] Token gecersiz -> 401: {result.Failure?.Message}");
            context.Response.StatusCode = 401;
            return; // next() cagrilmaz, Ocelot'a gitmez
        }
    }

    if (context.Request.Path.Equals("/webrtc.html", StringComparison.OrdinalIgnoreCase))
    {
        // Query string'i aynen ta��
        var qs = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : "";
        var target = "http://10.1.180.10:1984/webrtc.html" + qs;

        // GET ise 302 yeterli; POST olsayd� 307/308 tercih ederdik
        context.Response.Redirect(target, permanent: false);
        return;
    }
    await next();
});

app.Use(async (context, next) =>
{
    var path = context.Request.Path;

    if (path.Equals("/api/shared/AlarmLog/ViyolYanginAlarmBildir", StringComparison.OrdinalIgnoreCase) || path.Equals("/shared/AlarmLog/ViyolYanginAlarmBildir", StringComparison.OrdinalIgnoreCase))
    {
        if (!HttpMethods.IsPost(context.Request.Method))
        {
            context.Response.StatusCode = 405;
            context.Response.StatusCode = 405;
            return;
        }

        var expectedKey = Environment.GetEnvironmentVariable("VIYOL_ALARM_API_KEY", EnvironmentVariableTarget.Machine);

        if (string.IsNullOrWhiteSpace(expectedKey))
        {
            context.Response.StatusCode = 503;
            return;
        }

        var headers = context.Request.Headers;

        if (!headers.TryGetValue("X-API-Key", out var key)
            || key.Count != 1)
        {
            context.Response.StatusCode = 401;
            return;
        }

        var expectedHash =
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(expectedKey));

        var incomingHash =
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(key[0] ?? ""));

        if (!System.Security.Cryptography.CryptographicOperations
            .FixedTimeEquals(expectedHash, incomingHash))
        {
            context.Response.StatusCode = 401;
            return;
        }
    }

    await next();
});

await app.UseOcelot(); // EN SONDA OLMALI
app.Run();