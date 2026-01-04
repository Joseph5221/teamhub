public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public Error? Error { get; }
    
    // Factory methods
    public static Result<T> Success(T value);
    public static Result<T> Failure(Error error);
}