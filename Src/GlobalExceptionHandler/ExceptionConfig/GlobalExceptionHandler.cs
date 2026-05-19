using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Security;
using System.Text.Json;
using System.Text.Json.Serialization;
namespace GlobalExceptionHandler.ExceptionConfig;

/// <summary>
/// handle the exception globaly
/// services.AddTransient<GlobalExceptionHandler>();//add this line DI Container
/// app.UseMiddleware<GlobalExceptionHandler>(); add this line to app middleware
/// </summary>
public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IMiddleware
{
    private readonly ILogger<GlobalExceptionHandler> _logger = logger;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next.Invoke(context);
        }
        catch (Exception ex)
        {
            if (context.Response.HasStarted)
            {
                _logger.LogWarning(ex, "The response has already started, the global exception handler cannot modify the response.");
                throw;
            }

            var (statusCode, errorType, errorMessage) = MapException(ex);

            context.Response.Clear();
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = statusCode;
            _logger.LogError(ex, "Unhandled exception caught by global handler");
            await context.Response.WriteAsync(JsonSerializer.Serialize(new ProblemDetails()
            {
                Status = statusCode,
                Title = errorType,
                Detail = errorMessage
            }, _jsonOptions));
        }
    }

    private static (int StatusCode, string ErrorType, string Message) MapException(Exception ex)
    {
        return ex switch
        {
            ValidationException ve => (StatusCodes.Status400BadRequest, nameof(ValidationException), ve.Message),
            ArgumentException ae => (StatusCodes.Status400BadRequest, nameof(ArgumentException), ae.Message),
            KeyNotFoundException knf => (StatusCodes.Status404NotFound, nameof(KeyNotFoundException), knf.Message),
            UnauthorizedAccessException ua => (StatusCodes.Status401Unauthorized, nameof(UnauthorizedAccessException), ua.Message),
            SecurityException se => (StatusCodes.Status403Forbidden, nameof(SecurityException), se.Message),
            OperationCanceledException oce => (StatusCodes.Status499ClientClosedRequest, "ClientCancelled", oce.Message),
            _ => (StatusCodes.Status500InternalServerError, "InternalServerError", ex.Message)
        };
    }
}
