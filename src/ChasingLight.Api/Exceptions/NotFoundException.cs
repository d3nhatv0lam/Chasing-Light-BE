namespace ChasingLight.Api.Exceptions;

public class NotFoundException : AppException
{
    private const string _defaultErrorCode = "RESOURCE_NOT_FOUND";

    public NotFoundException(string message, string errorCode = _defaultErrorCode)
        : base(message, errorCode, StatusCodes.Status404NotFound)
    {
    }

    public NotFoundException(string resourceName, object key, string errorCode = _defaultErrorCode)
        : base($"Resource '{resourceName}' with ID ({key}) was not found.", errorCode,
            StatusCodes.Status404NotFound)
    {
    }

    public NotFoundException(string message, Exception innerException, string errorCode = _defaultErrorCode)
        : base(message, innerException, errorCode, StatusCodes.Status404NotFound)
    {
    }
}
