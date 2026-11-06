using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog;
using System.ComponentModel.DataAnnotations;
using System.Net;

namespace MatliOtomasyon.Shared.Web.Errors
{
    /// <summary>
    /// API'lerde yakalanmamış hataları tek biçimde JSON cevaba çevirir. Doğrulama hataları 400, diğer tüm
    /// hatalar 500 döner. 500 cevabında hata kimliği (<c>Guid</c>), istek yolu ve hata mesajı bulunur;
    /// stack trace istemciye gönderilmez, sadece loga yazılır. Faz 2'de ProblemDetails tabanlı
    /// <c>IExceptionHandler</c> ile değiştirilecek.
    /// </summary>
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        /// <summary>
        /// Middleware zincirindeki sonraki adımı ve logger'ı alır.
        /// </summary>
        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        /// <summary>
        /// <c>UseExceptionHandler</c> tarafından yakalanan hatayı loglar ve istemciye stack trace içermeyen
        /// JSON hata cevabını yazar. Loglardaki hata kaydı, cevaptaki <c>Guid</c> ile eşleştirilebilir.
        /// </summary>
        public async Task Invoke(HttpContext context)
        {
            context.Response.Clear();
            context.Response.ContentType = "application/json";

            var exceptionHandlerFeature = context.Features.Get<IExceptionHandlerFeature>();
            if (exceptionHandlerFeature == null)
                return;

            var exception = exceptionHandlerFeature.Error;
            string path = context.Request.Path;
            string jsonResult;

            if (exception is ValidationException validationException)
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                jsonResult = JsonConvert.SerializeObject(new
                {
                    ErrorMessages = validationException.Message
                });
            }
            else
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                exception.Data["path"] = path;

                var exceptionId = Guid.NewGuid().ToString();
                var eventId = new EventId(0, exceptionId);
                _logger.LogError(eventId, exception, "Unhandled exception {ExceptionId} on {Path}", exceptionId, path);
                Log.Error(exception, "Unhandled exception {ExceptionId} on {Path}", exceptionId, path);

                jsonResult = JsonConvert.SerializeObject(new
                {
                    Guid = exceptionId,
                    Path = path,
                    ErrorMessage = exception.Message
                });
            }

            await context.Response.WriteAsync(jsonResult);
        }
    }

    /// <summary>
    /// <see cref="ExceptionMiddleware"/>'i uygulamanın hata yakalayıcısı olarak kaydeden extension.
    /// </summary>
    public static class ExceptionMiddlewareExtensions
    {
        /// <summary>
        /// <c>UseExceptionHandler</c> ile yakalanan hataların <see cref="ExceptionMiddleware"/> üzerinden cevaplanmasını sağlar.
        /// </summary>
        public static void ConfigureExceptionHandler(this IApplicationBuilder app)
        {
            app.UseExceptionHandler(appError => appError.UseMiddleware<ExceptionMiddleware>());
        }
    }
}
