namespace ChasingLight.Api.Exceptions;

public class AppException : Exception
{
    private const string _defaultErrorCode = "BUSINESS_ERROR";
    public int StatusCode { get; init; }
    public string ErrorCode { get; init; }

    public AppException(string message, 
        string errorCode = _defaultErrorCode,
        int statusCode = 400) : base(message)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
    }

    public AppException(string message, Exception innerException, string errorCode = _defaultErrorCode,
        int statusCode = 400)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
    }
}