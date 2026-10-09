namespace ChasingLight.Api.DTOs.Common;

public record ErrorResponse
{
    public int StatusCode { get; init; }
    public string ErrorCode { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    /// <summary>
    /// detail for dev mode
    /// </summary>
    public string? Detail { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    public ErrorResponse(int statusCode, string errorCode, string message, string? detail = null)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
        Detail = detail;
        Message = message;
    }
}