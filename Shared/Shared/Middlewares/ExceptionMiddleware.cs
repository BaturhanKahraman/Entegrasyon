using Microsoft.AspNetCore.Http;
using System.Net;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace Shared.Middlewares
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;

        public ExceptionMiddleware(RequestDelegate request,ILogger<ExceptionMiddleware> logger)
        {
            _next = request;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next.Invoke(context);
            }
            catch(Exception e)
            {
                await HandleExceptionAsync(context,e);
            }
        }

        private async Task HandleExceptionAsync(HttpContext httpContext,Exception e)
        {
            httpContext.Response.ContentType = "application/json";
            httpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            string message = "Sistemsel bir hata oluşmuştur.";
            if(e is ValidationException validation)
            {
                httpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                //_logger.LogWarning(e,await httpContext.GetRequestInfosAsync());
                message =string.Join(Environment.NewLine,validation.Errors) ;
            }
            if(e.GetType() == typeof(TaskCanceledException))
            {
                message = e.Message;
                httpContext.Response.StatusCode =StatusCodes.Status410Gone;
            }
            else
            {
                _logger.LogError(e.ToString());
            }

            await httpContext.Response.WriteAsync(message);
        }
    }
}