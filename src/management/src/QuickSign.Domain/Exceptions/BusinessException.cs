namespace QuickSign.Domain.Exceptions;

/// <summary>
/// Domain and business exception carrying standardized SCREAMING_SNAKE_CASE error codes.
/// Serialized into the unified backend JSON error response by the exception middleware.
/// </summary>
public class BusinessException : Exception
{
    public string ErrorCode { get; }
    public int HttpStatus { get; }
    public IDictionary<string, object>? Details { get; }

    public BusinessException(
        string errorCode,
        string message,
        int httpStatus = 400,
        IDictionary<string, object>? details = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        HttpStatus = httpStatus;
        Details = details;
    }
}
