using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog;
using SharedKernel.Extensions;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text;

namespace SharedKernel.Middlewares
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            context.Response.Clear();
            context.Response.ContentType = "application/json";

            IExceptionHandlerFeature exceptionHandlerFeature = context.Features.Get<IExceptionHandlerFeature>();

            if (exceptionHandlerFeature == null)
                return;

            Exception ex = exceptionHandlerFeature.Error;
            string path = context.Request.Path;

            string jsonResult = "";
            if (!(ex is ValidationException))
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                ex.Data.Add("path", path);

                string exceptionId = Guid.NewGuid().ToString();

                EventId eventId = new EventId(0, exceptionId);
                _logger.LogError(eventId: eventId,
                    exception: ex,
                    message: $"Date : {DateTime.Now.ToString()} \nPath : {path} \nStackTrace: {ex.ToString()}");

                Log.Error($"eventId: {eventId}, exception: {ex}, message: 'Date : {DateTime.Now.ToString()} \nPath : {path} \nStackTrace: {ex.ToString()}'");

                jsonResult = JsonConvert.SerializeObject(new
                {
                    Guid = exceptionId,
                    Path = path,
                    ErrorMessage = ex.Message,
                    ex.StackTrace,
                });
            }
            else
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                ValidationException valException = (ValidationException)ex;

                jsonResult = JsonConvert.SerializeObject(new
                {
                    ErrorMessages = valException.Message
                });
            }

            await context.Response.WriteAsync(jsonResult);
        }

        private string GetJsonOfException(HttpContext context, Exception exception, string requestBodyAsString)
        {
            return exception switch
            {
                //ValidationException valException => HandleValidationException(context, valException),
                BusinessException busException => HandleBusinessException(context, busException),
                _ => HandleException(context, exception, requestBodyAsString),
            };
        }

        //private string HandleValidationException(HttpContext context, ValidationException valException)
        //{
        //    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;

        //    return JsonConvert.SerializeObject(new
        //    {
        //        ErrorMessage = valException.Errors.Select(x => x.ErrorMessage).FirstOrDefault()
        //    });
        //}

        private string HandleBusinessException(HttpContext context, BusinessException busException)
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;

            return JsonConvert.SerializeObject(new
            {
                ErrorMessage = busException.Message
            });
        }

        private string HandleException(HttpContext context, Exception ex, string requestBodyAsString)
        {
            string path = context.Request.Path;

            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            ex.Data.Add("path", path);

            string exceptionId = Guid.NewGuid().ToString();

            EventId eventId = new EventId(0, exceptionId);
            _logger.LogError(eventId: eventId,
                exception: ex,
                message: $"Date : {DateTime.Now} \nPath : {path} \nStackTrace: {ex}\n{requestBodyAsString}");

            return JsonConvert.SerializeObject(new
            {
                Guid = exceptionId,
                Path = path,
                ErrorMessage = ex.Message,
                ex.StackTrace,
            });
        }

        private async Task<string> FormatRequest(HttpRequest request)
        {
            try
            {
                request.EnableBuffering();

                var buffer = new byte[Convert.ToInt32(request.ContentLength)];

                await request.Body.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);

                var bodyAsText = Encoding.UTF8.GetString(buffer);

                request.Body.Position = 0;

                return bodyAsText;
            }
            catch
            {
                return string.Empty;
            }
        }
    }

    //https://code-maze.com/global-error-handling-aspnetcore/
    public static class ExceptionMiddlewareExtensions
    {
        public static void ConfigureExceptionHandler(this IApplicationBuilder app)
        {
            app.UseExceptionHandler(appError => appError.UseMiddleware<ExceptionMiddleware>());
        }
    }
}
