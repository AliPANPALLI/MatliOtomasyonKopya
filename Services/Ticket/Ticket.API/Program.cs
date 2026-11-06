using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Ticket.Application.Mapster;
using Ticket.Infrastructure;
using MatliOtomasyon.Shared.Infrastructure.Configuration;
using MatliOtomasyon.Shared.Web.Errors;
using MatliOtomasyon.Shared.Web.Security;

var builder = WebApplication.CreateBuilder(args);
var dbUser = Environment.GetEnvironmentVariable("DB_USER");
var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");

var connectionString = builder.Configuration.GetConnectionString("SqlServer")
    .Replace("{DB_USER}", dbUser)
    .Replace("{DB_PASSWORD}", dbPassword);
builder.Services.AddDbContext<TicketDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer"), sqlOptions =>
    {
        sqlOptions.MigrationsAssembly("Ticket.Infrastructure");
    });
});

// Add services to the container.
builder.Services.Configure<CustomTokenOption>(builder.Configuration.GetSection("TokenOption"));
builder.Services.AddHttpContextAccessor();

//authentication
builder.Services.AddCustomTokenAuth(builder.Configuration.GetSection("TokenOption").Get<CustomTokenOption>());

builder.Services.AddAuthorization();

#region DI
//DI Register
builder.Services.RegisterServices();
#region Mapster
var mapsterConfig = new TypeAdapterConfig();
mapsterConfig.Scan(typeof(TicketMappingConfig).Assembly);
mapsterConfig.Compile();
builder.Services.AddSingleton(mapsterConfig);
builder.Services.AddScoped<IMapper, ServiceMapper>();
#endregion Mapster
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "Ticket.API", Version = "v1" })
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
using (var serviceScope = app.Services.GetRequiredService<IServiceScopeFactory>().CreateScope())
{
    var context = serviceScope.ServiceProvider.GetRequiredService<TicketDbContext>();
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

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

//app.UseSession();

app.Run();
