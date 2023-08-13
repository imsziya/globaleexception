namespace GlobalExceptionHandler.Models;

public record ErrorResponse(int StatusCode, string ErrorType, string ErrorMessage);
