using ChasingLight.Api.DTOs.Common;
using ChasingLight.Api.Exceptions;

namespace ChasingLight.Api.Infrastructures.Middlewares;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionMiddleware(RequestDelegate next,
        ILogger<ExceptionMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        int statusCode;
        string errorCode;
        string message;

        switch (exception)
        {
            case AppException appEx:
                statusCode = appEx.StatusCode;
                errorCode = appEx.ErrorCode;
                message = appEx.Message;
                _logger.LogWarning("Nghiệp vụ: [{Code}] {Message}", errorCode, message);
                break;

            case KeyNotFoundException:
                statusCode = StatusCodes.Status404NotFound;
                errorCode = "RESOURCE_NOT_FOUND";
                message = exception.Message;
                _logger.LogWarning("Không tìm thấy dữ liệu: {Message}", message);
                break;
            
            case ArgumentException argEx:
                statusCode = StatusCodes.Status400BadRequest;
                errorCode = "INVALID_ARGUMENT";
                message = argEx.Message;
                _logger.LogWarning("Tham số không hợp lệ: {Message}", message);
                break;
            
            case UnauthorizedAccessException:
                statusCode = StatusCodes.Status401Unauthorized;
                errorCode = "UNAUTHORIZED";
                message = "Bạn không có quyền truy cập tài nguyên này!";
                _logger.LogWarning("Truy cập trái phép: {Message}", exception.Message);
                break;

            default:
                statusCode = StatusCodes.Status500InternalServerError;
                errorCode = "INTERNAL_SERVER_ERROR";
                message = "Lỗi hệ thống!";
                _logger.LogError(exception, "Lỗi server: {Message}", exception.Message);
                break;
        }
        
        context.Response.StatusCode = statusCode;
        var detail = _env.IsDevelopment() ? exception.ToString() : null;
        var errorResponse = new ErrorResponse(statusCode,errorCode, message, detail);
        
        await context.Response.WriteAsJsonAsync(errorResponse);

    }
}