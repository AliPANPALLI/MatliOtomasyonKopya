using WeighbridgeTracking.Application.Exceptions;
using WeighbridgeTracking.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace WeighbridgeTracking.API.Filters
{
    // Servis/repository exception'larını HTTP yanıtına çevirir.
    // Yanıt gövdesi her durumda { message } şeklindedir; stack trace dönülmez.
    public sealed class KantarTakipExceptionFilter : IExceptionFilter
    {
        private readonly ILogger<KantarTakipExceptionFilter> _logger;

        public KantarTakipExceptionFilter(ILogger<KantarTakipExceptionFilter> logger)
        {
            _logger = logger;
        }

        public void OnException(ExceptionContext context)
        {
            var ex = context.Exception;

            if (ex is OperationCanceledException &&
                context.HttpContext.RequestAborted.IsCancellationRequested)
            {
                // İstemci isteği iptal etti.
                context.Result = new StatusCodeResult(499);
                context.ExceptionHandled = true;
                return;
            }

            switch (ex)
            {
                case KayitBulunamadiException:
                    SetResult(context, StatusCodes.Status404NotFound, new { message = ex.Message });
                    return;

                case IsKuraliException:
                    SetResult(context, StatusCodes.Status409Conflict, new { message = ex.Message });
                    return;

                case GecersizIstekException:
                    SetResult(context, StatusCodes.Status400BadRequest, new { message = ex.Message });
                    return;

                case SapEntegrasyonException:
                    _logger.LogWarning(ex, "SAP entegrasyon hatası. Path: {Path}", context.HttpContext.Request.Path);
                    SetResult(context, StatusCodes.Status502BadGateway, new { message = ex.Message });
                    return;
            }

            var hataId = Guid.NewGuid().ToString();

            _logger.LogError(ex, "Beklenmeyen hata. HataId: {HataId}, Path: {Path}", hataId, context.HttpContext.Request.Path);

            SetResult(context, StatusCodes.Status500InternalServerError, new
            {
                message = "Beklenmeyen bir hata oluştu.",
                hataId
            });
        }

        private static void SetResult(ExceptionContext context, int statusCode, object body)
        {
            context.Result = new ObjectResult(body) { StatusCode = statusCode };
            context.ExceptionHandled = true;
        }
    }
}
