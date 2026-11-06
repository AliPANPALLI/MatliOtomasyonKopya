using Microsoft.AspNetCore.Diagnostics;
using SharedKernel.Extensions;
using System.ComponentModel.DataAnnotations;
using System.Net;

namespace MobilGuncelleme.API.Middlewares
{
    /// <summary>
    /// Bu API'nin hata cevabı. SharedKernel'deki ExceptionMiddleware istemciye stack trace döndürür
    /// ve BusinessException'ı 500 olarak işler; mobil güncelleme dokümanı production'da stack trace
    /// dönülmemesini istediği için burada ayrı tutulur:
    /// BusinessException / ValidationException → 400 + mesaj, diğerleri → 500 + yalnızca hata kimliği.
    /// Ayrıntı yalnızca loga yazılır.
    /// </summary>
    public class HataYonetimiMiddleware
    {
        private readonly ILogger<HataYonetimiMiddleware> _logger;

        public HataYonetimiMiddleware(RequestDelegate _, ILogger<HataYonetimiMiddleware> logger)
        {
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            var ex = context.Features.Get<IExceptionHandlerFeature>()?.Error;
            if (ex is null)
                return;

            if (ex is BusinessException or ValidationException)
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                await context.Response.WriteAsJsonAsync(new { errorMessage = ex.Message });
                return;
            }

            var hataId = Guid.NewGuid().ToString();
            _logger.LogError(ex, "Hata {HataId} Path: {Path}", hataId, context.Request.Path);

            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            await context.Response.WriteAsJsonAsync(new { guid = hataId, errorMessage = "Beklenmeyen bir hata oluştu." });
        }
    }
}
