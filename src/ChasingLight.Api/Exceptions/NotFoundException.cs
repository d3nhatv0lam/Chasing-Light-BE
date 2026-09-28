namespace ChasingLight.Api.Exceptions;

public class NotFoundException : AppException
{
    private const string _defaultErrorCode = "RESOURCE_NOT_FOUND";

    public NotFoundException(string message, string errorCode = _defaultErrorCode)
        : base(message, errorCode, StatusCodes.Status404NotFound)
    {
    }

    public NotFoundException(string resourceName, object key, string errorCode = _defaultErrorCode)
        : base($"Không tìm thấy tài nguyên '{resourceName}' với định danh ({key})!", errorCode,
            StatusCodes.Status404NotFound)
    {
    }

    public NotFoundException(string message, Exception innerException, string errorCode = _defaultErrorCode)
        : base(message, innerException, errorCode, StatusCodes.Status404NotFound)
    {
    }
}