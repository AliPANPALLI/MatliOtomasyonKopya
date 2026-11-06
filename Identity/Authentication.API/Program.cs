using Authentication.API.Security;
using Authentication.Application.Mapster;
using Microsoft.AspNetCore.RateLimiting;
using Authentication.Infrastructure;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Configurations;
using SharedKernel.Extensions;
using SharedKernel.Middlewares;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<Authentication.API.Security.AuthChallenges>();
builder.Services.Configure<Microsoft.AspNetCore.Builder.ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor |
        Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 1;
    // Loopback is trusted by default. Add remote gateway addresses explicitly through configuration.
    foreach (var address in builder.Configuration.GetSection("TrustedProxies").Get<string[]>() ?? Array.Empty<string>())
        options.KnownProxies.Add(System.Net.IPAddress.Parse(address));
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.AddPolicy("authentication", context =>
        System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ =>
                new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
                {
                    PermitLimit = 60, Window = TimeSpan.FromMinutes(1), QueueLimit = 0,
                    AutoReplenishment = true
                }));
    options.OnRejected = async (context, cancellationToken) =>
        await context.HttpContext.Response.WriteAsJsonAsync(
            new { ErrorMessage = "Çok fazla istek. Lütfen daha sonra tekrar deneyin." }, cancellationToken);
});
var dbUser = Environment.GetEnvironmentVariable("DB_USER");
var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");

var connectionString = builder.Configuration.GetConnectionString("SqlServer")
    .Replace("{DB_USER}", dbUser)
    .Replace("{DB_PASSWORD}", dbPassword);
builder.Services.AddDbContext<AuthDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer"), sqlOptions =>
    {
        sqlOptions.MigrationsAssembly("Authentication.Infrastructure");
    });
});

// Add services to the container.
builder.Services.Configure<CustomTokenOption>(builder.Configuration.GetSection("TokenOption"));
builder.Services.AddHttpContextAccessor();

//authentication
builder.Services.AddCustomTokenAuth(builder.Configuration.GetSection("TokenOption").Get<CustomTokenOption>());

builder.Services.Configure<UserManagementOptions>(builder.Configuration.GetSection(UserManagementOptions.SectionName));
builder.Services.Configure<InternalServiceOptions>(builder.Configuration.GetSection(InternalServiceOptions.SectionName));
builder.Services.AddHttpClient();
builder.Services.AddScoped<IAuthorizationHandler, UserManagementAuthorizationHandler>();
builder.Services.AddAuthorization(options =>
{
    // Yetki işaretlenmemiş her uç kimlik doğrulaması ister; anonim uçlar [AllowAnonymous] ile açıkça işaretlenir.
    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
    options.AddPolicy(UserManagementPolicies.Read, policy =>
        policy.AddRequirements(new UserManagementRequirement(requiresWrite: false, allowInternalService: false)));
    options.AddPolicy(UserManagementPolicies.Write, policy =>
        policy.AddRequirements(new UserManagementRequirement(requiresWrite: true, allowInternalService: false)));
    options.AddPolicy(UserManagementPolicies.ReadOrInternalService, policy =>
        policy.AddRequirements(new UserManagementRequirement(requiresWrite: false, allowInternalService: true)));
});

#region DI
//DI Register


builder.Services.RegisterServices();
#region Mapster
var mapsterConfig = new TypeAdapterConfig();
mapsterConfig.Scan(typeof(AuthMappingConfig).Assembly);
mapsterConfig.Compile();
builder.Services.AddSingleton(mapsterConfig);
builder.Services.AddScoped<MapsterMapper.IMapper, ServiceMapper>();
#endregion Mapster
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "Authentication.API", Version = "v1" })
);
#endregion DI

#region
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
#endregion cors

var app = builder.Build();
app.UseForwardedHeaders();
using (var serviceScope = app.Services.GetRequiredService<IServiceScopeFactory>().CreateScope())
{
    var context = serviceScope.ServiceProvider.GetRequiredService<AuthDbContext>();
    var result = context.Database.EnsureCreated();
}

// Configure the HTTP request pipeline.
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
app.UseRateLimiter();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

//app.UseSession();

app.Run();
