using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using NesneIzleme.Infrastructure;
using SharedKernel.Middlewares;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration
    .GetConnectionString("SqlServer")
    ?? throw new InvalidOperationException(
        "ConnectionStrings:SqlServer bulunamadý.");

builder.Services.AddDbContext<NesneIzlemeDbContext>(options =>
{
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null);
    });
});

builder.Services.RegisterServices();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc(
        "v1",
        new OpenApiInfo
        {
            Title = "NesneIzleme.API",
            Version = "v1"
        });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowOrigin", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

// Hata varsa tarayýcýda ve Visual Studio Output'ta açýkça görebilmek için
app.UseDeveloperExceptionPage();

// Swagger'ý ortamdan baðýmsýz olarak aç
app.UseSwagger();

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint(
        "/swagger/v1/swagger.json",
        "Nesne Ýzleme API v1");

    options.RoutePrefix = "swagger";
});

// Þimdilik ExceptionMiddleware'i kapat.
// Swagger çalýþtýktan sonra tekrar ekleriz.
// app.UseMiddleware<ExceptionMiddleware>();

app.UseCors("AllowOrigin");

app.UseAuthentication();
app.UseAuthorization();

// Controller ve servislerden baðýmsýz test adresi
app.MapGet("/api/test", () =>
{
    return Results.Ok(new
    {
        durum = true,
        mesaj = "Nesne Ýzleme API çalýþýyor."
    });
}).AllowAnonymous();

app.MapControllers();

// Ana adrese girilirse Swagger'a yönlendir
app.MapGet("/", () => Results.Redirect("/swagger/index.html"));

app.Run();