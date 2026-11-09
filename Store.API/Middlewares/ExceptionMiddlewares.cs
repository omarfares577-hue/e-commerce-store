using Store.API.Errors;
using System.Text.Json;
using System.Net;
using System.Security.Cryptography.Xml;
namespace Store.API.Middlewares
{
    public class ExceptionMiddlewares
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddlewares> _logger;
        private readonly IHostEnvironment _env;
        public ExceptionMiddlewares(RequestDelegate next, ILogger<ExceptionMiddlewares> logger, IHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context) ;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = 500;
                var respnse = (_env.IsDevelopment() || _env.IsEnvironment("Testing"))?
                  new  ApiException((int)HttpStatusCode.InternalServerError,ex.Message,ex.StackTrace?.ToString()) : 
                  new  ApiException((int)HttpStatusCode.InternalServerError);
                var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                var json =JsonSerializer.Serialize(respnse, options);
                await context.Response.WriteAsync(json);
            }
            
        }
    }
}
