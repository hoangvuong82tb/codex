namespace ASMILE.BO.Shared.Exceptions;

public abstract class AppException : Exception
{
    protected AppException(string errorCode, string message, Exception? innerException = null) : base(message, innerException)
    {
        ErrorCode = errorCode;
    }

    public string ErrorCode { get; }
}

public static class ErrorCodes
{
    public const string ValidationFailed = "VALIDATION_FAILED";
    public const string BusinessRuleFailed = "BUSINESS_RULE_FAILED";
    public const string NotFound = "NOT_FOUND";
    public const string ConcurrencyConflict = "CONCURRENCY_CONFLICT";
    public const string ConfigurationInvalid = "CONFIGURATION_INVALID";
}

public sealed class BusinessException : AppException
{
    public BusinessException(string message, string errorCode = ErrorCodes.BusinessRuleFailed, Exception? innerException = null)
        : base(errorCode, message, innerException)
    {
    }
}

public sealed class AppNotFoundException : AppException
{
    public AppNotFoundException(string message, Exception? innerException = null)
        : base(ErrorCodes.NotFound, message, innerException)
    {
    }
}

public sealed class AppConcurrencyException : AppException
{
    public AppConcurrencyException(string message, Exception? innerException = null)
        : base(ErrorCodes.ConcurrencyConflict, message, innerException)
    {
    }
}
