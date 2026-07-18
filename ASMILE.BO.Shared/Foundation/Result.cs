namespace ASMILE.BO.Shared.Foundation;

public enum ErrorCategory { Validation, Business, Authorization, Concurrency, IntegrationPending, UnknownExternalOutcome, System }
public enum ErrorSeverity { Info, Warning, Error, Critical }

public sealed record Error(
    string Code,
    string Message,
    ErrorCategory Category,
    ErrorSeverity Severity = ErrorSeverity.Error,
    bool Retryable = false,
    IReadOnlyDictionary<string, string>? Metadata = null)
{
    public static Error Validation(string code, string message) => new(code, message, ErrorCategory.Validation);
    public static Error Business(string code, string message) => new(code, message, ErrorCategory.Business);
    public static Error Authorization(string code, string message) => new(code, message, ErrorCategory.Authorization);
    public static Error Concurrency(string code, string message) => new(code, message, ErrorCategory.Concurrency, ErrorSeverity.Warning, true);
    public static Error System(string code, string message, bool retryable = false) => new(code, message, ErrorCategory.System, ErrorSeverity.Error, retryable);
}

public class Result
{
    protected Result(bool isSuccess, Error? error) { IsSuccess = isSuccess; Error = error; }
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error? Error { get; }
    public static Result Success() => new(true, null);
    public static Result Failure(Error error) => new(false, error ?? throw new ArgumentNullException(nameof(error)));
}

public sealed class Result<T> : Result
{
    private Result(T value) : base(true, null) { Value = value; }
    private Result(Error error) : base(false, error) { Value = default; }
    public T? Value { get; }
    public static Result<T> Success(T value) => new(value);
    public new static Result<T> Failure(Error error) => new(error);
}
