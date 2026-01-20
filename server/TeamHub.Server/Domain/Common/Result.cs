namespace TeamHub.Server.Domain.Common;

/// <summary>
/// Represents the result of an operation that can succeed or fail
/// </summary>
public class Result<T>
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public T? Value { get; }
    public Error? Error { get; }

    private Result(bool isSuccess, T? value, Error? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    /// <summary>
    /// Creates a successful result
    /// </summary>
    public static Result<T> Success(T value) => new(true, value, null);

    /// <summary>
    /// Creates a failed result
    /// </summary>
    public static Result<T> Failure(Error error) => new(false, default, error);
}

/// <summary>
/// Represents a result without a return value
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error? Error { get; }

    private Result(bool isSuccess, Error? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>
    /// Creates a successful result
    /// </summary>
    public static Result Success() => new(true, null);

    /// <summary>
    /// Creates a failed result
    /// </summary>
    public static Result Failure(Error error) => new(false, error);
}