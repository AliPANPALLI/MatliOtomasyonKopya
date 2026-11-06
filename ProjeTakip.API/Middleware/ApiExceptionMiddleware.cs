using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using ProjeTakip.Application.Exceptions;

namespace ProjeTakip.API.Middleware;

public sealed class ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger, IHostEnvironment environment)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            var status = exception switch
            {
                ProjeTakipValidationException => StatusCodes.Status400BadRequest,
                ProjeTakipNotFoundException => StatusCodes.Status404NotFound,
                ProjeTakipConflictException => StatusCodes.Status409Conflict,
                ProjeTakipForbiddenException => StatusCodes.Status403Forbidden,
                DbUpdateConcurrencyException => StatusCodes.Status409Conflict,
                DbUpdateException => StatusCodes.Status409Conflict,
                BadHttpRequestException => StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status500InternalServerError
            };
            if (status == StatusCodes.Status500InternalServerError)
                logger.LogError(exception, "Proje Takip API işlenmeyen hata üretti.");
            else
                logger.LogWarning(exception, "Proje Takip isteği reddedildi: {Message}", exception.Message);

            context.Response.StatusCode = status;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = status,
                Title = status == 500 ? "İşlem tamamlanamadı." : exception.Message,
                Detail = status == 500 && !environment.IsDevelopment() ? "Beklenmeyen bir sunucu hatası oluştu." : exception.Message,
                Instance = context.Request.Path
            });
        }
    }
}
