namespace ASMILE.BO.Shared.Exceptions;

public sealed class AppValidationException : AppException
{
    public AppValidationException(string message) : base(ErrorCodes.ValidationFailed, message)
    {
    }
}
