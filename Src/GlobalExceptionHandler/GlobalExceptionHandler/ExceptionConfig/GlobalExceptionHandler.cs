using GlobalExceptionHandler.Models;
using Newtonsoft.Json;

namespace GlobalExceptionHandler.ExceptionConfig;

/// <summary>
/// handle the exception globaly
/// services.AddTransient<GlobalExceptionHandler>();//add this line DI Container
/// app.UseMiddleware<GlobalExceptionHandler>(); add this line to app middleware
/// </summary>
public class GlobalExceptionHandler : IMiddleware
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next.Invoke(context);
        }
        catch (Exception ex)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            _logger.LogError("Error Message => {Message}", ex.Message);
            _logger.LogError("Error Message => {StackTrace}", ex.StackTrace);
            var response = new ErrorResponse(StatusCodes.Status500InternalServerError, "Internal Server", ex.Message);
            await context.Response.WriteAsync(JsonConvert.SerializeObject(response));
        }
    }
}
